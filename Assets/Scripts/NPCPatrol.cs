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

    [Header("Animation")]
    public Animator animator;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Go to the first waypoint if there are any
        if (waypoints.Length > 0 && waypoints[0] != null)
        {
            agent.SetDestination(waypoints[0].position);
        }
        else
        {
            Debug.LogWarning("NPC has no waypoints assigned! Please assign them in the inspector.");
        }
    }

    void Update()
    {
        // Update the animator based on the NavMeshAgent's speed
        if (animator != null)
        {
            // If the agent's velocity is greater than 0.1, it's moving
            animator.SetBool("IsMoving", agent.velocity.magnitude > 0.1f);
        }

        // If no waypoints are set, stop executing patrol logic
        if (waypoints.Length == 0) return;

        // Check if we reached the current waypoint destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!isWaiting)
            {
                // Start waiting
                isWaiting = true;
                waitTimer = waitTimeAtWaypoint;
            }
            else
            {
                // Count down the wait timer
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    // Timer finished, go to next waypoint
                    isWaiting = false;
                    GoToNextWaypoint();
                }
            }
        }
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        // Move to the next index, loop back to 0 if at the end of the array
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        
        if (waypoints[currentWaypointIndex] != null)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }
}
