using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCRandomWander : MonoBehaviour
{
    public float wanderRadius = 10f;
    public float wanderTimer = 5f;
    
    // Reference to Animator for playing animations
    public Animator animator;

    private NavMeshAgent agent;
    private float timer;

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // If the timer exceeds wanderTimer, pick a new random location
        if (timer >= wanderTimer)
        {
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }

        // Update the animator based on whether the agent is actually moving
        if (animator != null)
        {
            // If the agent is moving (velocity > 0.1), play walking animation
            if (agent.velocity.magnitude > 0.1f)
            {
                animator.SetBool("IsMoving", true);
            }
            else
            {
                animator.SetBool("IsMoving", false);
            }
        }
    }

    // Helper method to find a random point on the NavMesh
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        // Sample the NavMesh to find a valid walkable point near the random direction
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }
}
