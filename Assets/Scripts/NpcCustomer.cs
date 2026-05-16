using UnityEngine;
using UnityEngine.AI;

public class NpcCustomer : MonoBehaviour
{
    public enum NpcState
    {
        Wandering,
        GoingToOrder,
        Ordering,
        GoingToTable,
        Eating,
        Leaving
    }

    [Header("State")]
    public NpcState currentState = NpcState.Wandering;

    [Header("Item Settings")]
    public string desiredItem = "HuTieu";
    public bool hasReceivedItem = false;

    [Header("Sitting Setup")]
    public string sittingStateName = "Sitting Idle";
    public string standingStateName = "Standing Idle";
    public bool autoAdjustHeight = false;
    public float sittingYOffset = 0f;

    [Header("After Eating - Leave")]
    public float waitTimeBeforeLeaving = 5f;
    [HideInInspector] public Transform despawnPoint;

    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float wanderCheckInterval = 5f;

    [Header("Rotation Settings")]
    [Tooltip("How fast the NPC turns to face its walk direction. Raise if it still moonwalks.")]
    public float rotationSpeed = 10f;

    [Header("Head Lock (Fix Rotation Bug)")]
    [Tooltip("Kéo bone 'mixamorig:Head' từ hierarchy NPC vào đây để khóa đầu khi ngồi")]
    public Transform headBone;
    private Quaternion lockedHeadLocalRotation;

    private Animator animator;
    private NavMeshAgent agent;
    private TableDelivery assignedTable;
    
    private float wanderTimer = 0f;
    private float leaveTimer = 0f;
    private bool isWaitingToLeave = false;
    private Quaternion sittingRotation; // Snapshot rotation khi ngồi xuống

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        
        // Disable old scripts if they exist to prevent conflicts
        var patrol = GetComponent<NPCPatrol>();
        if (patrol != null) patrol.enabled = false;
        
        var randomWander = GetComponent<NPCRandomWander>();
        if (randomWander != null) randomWander.enabled = false;
    }

    void Start()
    {
        SwitchState(NpcState.Wandering);
    }

    void Update()
    {
        UpdateAnimator();

        switch (currentState)
        {
            case NpcState.Wandering:
                UpdateWandering();
                break;
            case NpcState.GoingToOrder:
                UpdateGoingToOrder();
                break;
            case NpcState.Ordering:
                // Waiting for NpcOrderInteract to call OnOrderConfirmed()
                break;
            case NpcState.GoingToTable:
                UpdateGoingToTable();
                break;
            case NpcState.Eating:
                UpdateEating();
                break;
            case NpcState.Leaving:
                UpdateLeaving();
                break;
        }
    }

    void UpdateAnimator()
    {
        if (animator != null && agent != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsMoving", isMoving);

            // Rotate to face movement direction - fixes the "moonwalk" bug
            if (isMoving && currentState != NpcState.Eating)
            {
                Vector3 moveDir = agent.velocity;
                moveDir.y = 0f;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                }
            }
        }

        // Lock root rotation when sitting
        if (currentState == NpcState.Eating)
        {
            transform.rotation = sittingRotation;
        }
    }

    // LateUpdate chạy SAU khi Animator xử lý xong — override head bone trực tiếp
    void LateUpdate()
    {
        if (currentState == NpcState.Eating && headBone != null)
        {
            headBone.localRotation = lockedHeadLocalRotation;
        }
    }

    public void SwitchState(NpcState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case NpcState.Wandering:
                wanderTimer = wanderCheckInterval;
                break;
            case NpcState.GoingToOrder:
                agent.isStopped = false;
                if (RestaurantManager.Instance != null && RestaurantManager.Instance.orderSpot != null)
                {
                    agent.SetDestination(RestaurantManager.Instance.orderSpot.position);
                }
                break;
            case NpcState.Ordering:
                agent.isStopped = true;
                break;
            case NpcState.GoingToTable:
                agent.isStopped = false;
                if (assignedTable != null && assignedTable.chairWaypoint != null)
                {
                    agent.SetDestination(assignedTable.chairWaypoint.position);
                }
                break;
            case NpcState.Eating:
                WaitAtPickupSpot(); // Stand idle at pickup spot (no sitting)
                break;
            case NpcState.Leaving:
                agent.isStopped = false;
                if (despawnPoint != null)
                {
                    agent.SetDestination(despawnPoint.position);
                }
                break;
        }
    }

    void UpdateWandering()
    {
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderCheckInterval)
        {
            wanderTimer = 0f;

            // Check if we can order food
            if (RestaurantManager.Instance != null && RestaurantManager.Instance.TryReserveOrderSpot())
            {
                SwitchState(NpcState.GoingToOrder);
                return;
            }

            // Otherwise, pick a random waypoint from the Restaurant Manager to walk to
            if (RestaurantManager.Instance != null)
            {
                Transform wanderDest = RestaurantManager.Instance.GetRandomWanderWaypoint();
                if (wanderDest != null)
                {
                    agent.SetDestination(wanderDest.position);
                    return;
                }
            }

            // Fallback if no waypoints are set up
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
        }
    }

    void UpdateGoingToOrder()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SwitchState(NpcState.Ordering);
        }
    }

    // Called by NpcOrderInteract when player presses E
    public void OnOrderConfirmed()
    {
        if (currentState == NpcState.Ordering)
        {
            if (RestaurantManager.Instance != null)
            {
                RestaurantManager.Instance.ReleaseOrderSpot();
                assignedTable = RestaurantManager.Instance.TryGetTable();
                
                if (assignedTable != null)
                {
                    assignedTable.linkedNpc = this;
                    SwitchState(NpcState.GoingToTable);
                }
                else
                {
                    // Failsafe: if no tables suddenly, go back to wandering
                    SwitchState(NpcState.Wandering);
                }
            }
        }
    }

    void UpdateGoingToTable()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            SwitchState(NpcState.Eating); // "Eating" state = standing and waiting for takeaway pickup
        }
    }

    // NPC arrives at the pickup spot and stands idle waiting for food
    public void WaitAtPickupSpot()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.updateRotation = false;
        agent.updatePosition = false;

        // Snapshot rotation so UpdateAnimator keeps them facing the right way
        sittingRotation = transform.rotation;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetBool("IsMoving", false);
            // Make sure they are NOT sitting - just stand idle
            animator.SetBool("IsSitting", false);
            animator.Play(standingStateName, 0, 0f);
        }

        if (assignedTable != null)
        {
            assignedTable.enabled = true;
        }
    }

    public bool ReceiveItem(string itemName)
    {
        // This is now handled entirely by TableDelivery via WalkAway()
        // Kept for compatibility
        return itemName == desiredItem;
    }

    void UpdateEating()
    {
        // NPC is standing idle at pickup spot - nothing to do, TableDelivery handles the interaction
    }

    // Called by TableDelivery after the thank-you dialogue closes
    public void WalkAway()
    {
        StandUpAndLeave();
    }

    public void StandUpAndLeave()
    {
        hasReceivedItem = false;
        isWaitingToLeave = false;

        // Bật lại rotation để NPC có thể di chuyển bình thường
        agent.updateRotation = true;
        agent.updatePosition = true;

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            
            // Tắt IsEating một cách an toàn
            foreach (var param in animator.parameters)
            {
                if (param.name == "IsEating" && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("IsEating", false);
                    break;
                }
            }
            
            animator.Play(standingStateName, 0, 0f);
        }

        if (assignedTable != null)
        {
            assignedTable.enabled = false;
            assignedTable.linkedNpc = null;
            
            if (RestaurantManager.Instance != null)
            {
                RestaurantManager.Instance.ReleaseTable(assignedTable);
            }
            assignedTable = null;
        }

        SwitchState(NpcState.Leaving);
    }

    void UpdateLeaving()
    {
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            Destroy(gameObject);
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }
}
