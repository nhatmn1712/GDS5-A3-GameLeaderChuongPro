using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [Tooltip("Add empty GameObjects here to act as waypoints for the NPC to follow.")]
    public Transform[] waypoints;
    [Tooltip("How long the NPC waits before moving to the next waypoint.")]
    public float waitTimeAtWaypoint = 2f;
    [Tooltip("If true, the NPC loops forever. If false, it stops at the last waypoint and calls WaitAtPickupSpot() on NpcCustomer.")]
    public bool loopPatrol = true;

    [Header("Animation")]
    public Animator animator;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool hasReachedEnd = false;
    private bool isPaused = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        GoToWaypoint(0);
    }

    void OnEnable()
    {
        // When re-enabled (e.g. after NPC stands up to leave), make sure agent is unpaused
        if (agent != null)
        {
            agent.isStopped = false;
        }

        // Only go to first waypoint if we actually have some assigned
        if (waypoints != null && waypoints.Length > 0)
        {
            hasReachedEnd = false;
            currentWaypointIndex = 0;
            GoToWaypoint(0);
        }
    }

    void Update()
    {
        // Update the animator based on the NavMeshAgent's speed
        if (animator != null)
        {
            animator.SetBool("IsMoving", agent.velocity.magnitude > 0.1f);
        }

        // If no waypoints are set or we reached the end of a non-looping path, do nothing
        if (waypoints == null || waypoints.Length == 0 || hasReachedEnd) return;

        // Check if we've arrived at the current waypoint
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                // Arrived — check if this is the last stop on a non-looping path
                if (!loopPatrol && currentWaypointIndex >= waypoints.Length - 1)
                {
                    hasReachedEnd = true;

                    // Tell the NpcCustomer to sit down
                    NpcCustomer customer = GetComponent<NpcCustomer>();
                    if (customer == null) customer = GetComponentInChildren<NpcCustomer>();
                    if (customer != null)
                    {
                        customer.WaitAtPickupSpot();
                    }
                    return;
                }

                // Start waiting at this waypoint
                isWaiting = true;
                waitTimer = waitTimeAtWaypoint;
            }
            else
            {
                if (isPaused) return; // Wait here until unpaused

                // Count down wait timer
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    GoToNextWaypoint();
                }
            }
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        GoToWaypoint(currentWaypointIndex);
    }

    void GoToWaypoint(int index)
    {
        if (waypoints == null || index >= waypoints.Length) return;
        if (waypoints[index] != null)
        {
            agent.SetDestination(waypoints[index].position);
        }
    }

    /// <summary>
    /// Called by NpcCustomer.StandUpAndLeave() to give the NPC a new set of waypoints to walk to.
    /// </summary>
    public void SetNewWaypoints(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
        hasReachedEnd = false;
        isWaiting = false;
        currentWaypointIndex = 0;
        loopPatrol = true; // Loop the leave waypoints so the NPC walks around normally again

        if (agent != null)
        {
            agent.isStopped = false;
        }

        GoToWaypoint(0);
        Debug.Log("[NPCPatrol] New waypoints assigned. NPC is walking away.");
    }

    public void PausePatrol()
    {
        isPaused = true;
    }

    public void ResumePatrol()
    {
        isPaused = false;
        // Reset timer so it moves to next waypoint immediately upon resume
        waitTimer = 0f; 
    }

    public int GetCurrentWaypointIndex()
    {
        return currentWaypointIndex;
    }

    public bool IsWaiting()
    {
        return isWaiting;
    }
}
