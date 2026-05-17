using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class GPSPathRenderer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How often to recalculate the path (in seconds) to save performance.")]
    public float updateInterval = 0.5f;
    [Tooltip("How high above the ground the line should hover (to avoid Z-fighting/clipping).")]
    public float yOffset = 0.2f;

    private LineRenderer lineRenderer;
    private NavMeshPath path;
    private float timer = 0f;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        path = new NavMeshPath();
        
        // Ensure the line renders on top of the ground correctly
        lineRenderer.enabled = false;
    }

    void Update()
    {
        // 1. Check if we actually have an active order
        if (GrabOrderManager.Instance == null || !GrabOrderManager.Instance.HasActiveOrder || GrabOrderManager.Instance.ActiveSpot == null)
        {
            if (lineRenderer.enabled)
                lineRenderer.enabled = false;
            return;
        }

        // 2. We have an order, so find the player (on foot or on bike)
        Transform playerTransform = GetActivePlayerTransform();
        if (playerTransform == null)
        {
            if (lineRenderer.enabled)
                lineRenderer.enabled = false;
            return;
        }

        // 3. Update the path periodically
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = updateInterval;
            CalculateAndDrawPath(playerTransform.position, GrabOrderManager.Instance.ActiveSpot.transform.position);
        }
    }

    Transform GetActivePlayerTransform()
    {
        // Check for walking player
        PlayerInteract pi = FindObjectOfType<PlayerInteract>();
        if (pi != null && pi.gameObject.activeInHierarchy)
        {
            return pi.transform;
        }

        // Check for bike
        BikeController bike = FindObjectOfType<BikeController>();
        if (bike != null && bike.gameObject.activeInHierarchy)
        {
            return bike.transform;
        }

        return null;
    }

    void CalculateAndDrawPath(Vector3 start, Vector3 end)
    {
        // Ask Unity's AI to find the shortest path on the roads/sidewalks
        if (NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
        {
            if (path.corners.Length > 1)
            {
                lineRenderer.enabled = true;
                lineRenderer.positionCount = path.corners.Length;

                // Apply points to the LineRenderer with a slight Y offset so it sits ON TOP of the road
                for (int i = 0; i < path.corners.Length; i++)
                {
                    Vector3 cornerPos = path.corners[i];
                    cornerPos.y += yOffset;
                    lineRenderer.SetPosition(i, cornerPos);
                }
            }
            else
            {
                // Path found but it's empty (maybe we are already there)
                lineRenderer.enabled = false;
            }
        }
        else
        {
            // Path not found (maybe they are flying outside the NavMesh)
            lineRenderer.enabled = false;
        }
    }
}
