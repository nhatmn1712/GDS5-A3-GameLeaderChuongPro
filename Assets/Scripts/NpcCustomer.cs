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

    private Animator animator;
    private NavMeshAgent agent;
    private TableDelivery assignedTable;
    
    private float wanderTimer = 0f;
    private float leaveTimer = 0f;
    private bool isWaitingToLeave = false;

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
            animator.SetBool("IsMoving", agent.velocity.magnitude > 0.1f);
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
                SitDown();
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
            SwitchState(NpcState.Eating);
        }
    }

    public void SitDown()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool("IsSitting", true);
            animator.SetBool("IsMoving", false);
            animator.Play(sittingStateName, 0, 0f);
        }

        if (autoAdjustHeight)
        {
            var pos = transform.localPosition;
            transform.localPosition = new Vector3(pos.x, sittingYOffset, pos.z);
        }

        if (assignedTable != null)
        {
            assignedTable.enabled = true;
        }
    }

    public bool ReceiveItem(string itemName)
    {
        if (!hasReceivedItem && itemName == desiredItem)
        {
            hasReceivedItem = true;
            Debug.Log("[NPC] Cảm ơn nhé! Hủ tiếu ngon quá!");

            if (animator != null)
            {
                foreach (var param in animator.parameters)
                {
                    if (param.name == "IsEating" && param.type == AnimatorControllerParameterType.Bool)
                    {
                        animator.SetBool("IsEating", true);
                        break;
                    }
                }
            }

            isWaitingToLeave = true;
            leaveTimer = waitTimeBeforeLeaving;

            return true;
        }
        return false;
    }

    void UpdateEating()
    {
        if (!isWaitingToLeave) return;

        leaveTimer -= Time.deltaTime;
        if (leaveTimer <= 0f)
        {
            isWaitingToLeave = false;
            StandUpAndLeave();
        }
    }

    void StandUpAndLeave()
    {
        hasReceivedItem = false;
        isWaitingToLeave = false;

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsEating", false);
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
