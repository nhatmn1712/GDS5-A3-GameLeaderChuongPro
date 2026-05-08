using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC customer that walks to the food cart, sits at a table,
/// waits for food, then stands up and leaves after eating.
/// </summary>
public class NpcCustomer : MonoBehaviour
{
    [Header("Item Settings")]
    public string desiredItem = "HuTieu";
    public bool hasReceivedItem = false;

    [Header("Sitting Setup")]
    [Tooltip("Name of the sitting animation state in the Animator Controller")]
    public string sittingStateName = "Sitting Idle";
    [Tooltip("Name of the idle/standing animation state in the Animator Controller")]
    public string standingStateName = "Standing Idle";
    public bool autoAdjustHeight = false;
    public float sittingYOffset = 0f;

    [Header("After Eating - Leave")]
    [Tooltip("How many seconds the NPC sits after receiving food before standing up.")]
    public float waitTimeBeforeLeaving = 5f;
    [Tooltip("Waypoints the NPC walks to AFTER eating and leaving the table.")]
    public Transform[] leaveWaypoints;

    [Header("Table Link")]
    [Tooltip("The TableDelivery script on this NPC's table. It will be enabled when the NPC sits down.")]
    public TableDelivery linkedTable;

    private Animator animator;
    private bool isSitting = false;
    private float leaveTimer = 0f;
    private bool isWaitingToLeave = false;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!isWaitingToLeave) return;

        leaveTimer -= Time.deltaTime;
        if (leaveTimer <= 0f)
        {
            isWaitingToLeave = false;
            StandUpAndLeave();
        }
    }

    /// <summary>Called by NPCPatrol when the NPC reaches the final (table) waypoint.</summary>
    public void SitDown()
    {
        isSitting = true;

        // Stop all movement
        var patrol = GetComponent<NPCPatrol>();
        if (patrol != null) patrol.enabled = false;

        var wander = GetComponentInParent<NPCRandomWander>();
        if (wander == null) wander = GetComponent<NPCRandomWander>();
        if (wander != null) wander.enabled = false;

        var navAgent = GetComponentInParent<NavMeshAgent>();
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
        }

        // Play sitting animation
        if (animator != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "IsSitting" && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("IsSitting", true);
                    animator.SetBool("IsMoving", false);
                    Debug.Log("[NpcCustomer] Set IsSitting = true");
                    break;
                }
            }
            animator.Play(sittingStateName, 0, 0f);
        }

        // Adjust height if needed
        if (autoAdjustHeight)
        {
            var pos = transform.localPosition;
            transform.localPosition = new Vector3(pos.x, sittingYOffset, pos.z);
        }

        // Activate the table so the player can deliver food
        if (linkedTable != null)
        {
            linkedTable.enabled = true;
            Debug.Log("[NpcCustomer] Table delivery enabled.");
        }
    }

    /// <summary>Called by TableDelivery when food is placed on the table.</summary>
    public bool ReceiveItem(string itemName)
    {
        if (!hasReceivedItem && itemName == desiredItem)
        {
            hasReceivedItem = true;
            Debug.Log("[NPC] Cảm ơn nhé! Hủ tiếu ngon quá!");

            // Trigger eating animation if available
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

            // Start countdown to leave
            isWaitingToLeave = true;
            leaveTimer = waitTimeBeforeLeaving;

            return true;
        }
        return false;
    }

    /// <summary>NPC stands up and walks away after finishing their food.</summary>
    void StandUpAndLeave()
    {
        isSitting = false;
        hasReceivedItem = false;
        isWaitingToLeave = false;

        // Play standing animation
        if (animator != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == "IsSitting" && param.type == AnimatorControllerParameterType.Bool)
                    animator.SetBool("IsSitting", false);
                if (param.name == "IsEating" && param.type == AnimatorControllerParameterType.Bool)
                    animator.SetBool("IsEating", false);
            }
            animator.Play(standingStateName, 0, 0f);
        }

        // Re-enable patrol with leave waypoints
        var navAgent = GetComponentInParent<NavMeshAgent>();
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.isStopped = false;
        }

        if (leaveWaypoints != null && leaveWaypoints.Length > 0)
        {
            var patrol = GetComponent<NPCPatrol>();
            if (patrol != null)
            {
                patrol.SetNewWaypoints(leaveWaypoints);
                patrol.enabled = true;
                Debug.Log("[NpcCustomer] NPC is leaving the table.");
            }
        }

        // Disable the table again since no one is sitting
        if (linkedTable != null)
        {
            linkedTable.enabled = false;
            Debug.Log("[NpcCustomer] Table delivery disabled (NPC left).");
        }
    }
}
