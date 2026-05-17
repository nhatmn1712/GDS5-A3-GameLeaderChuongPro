using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to an Image UI element in the main Canvas.
/// Shows a screen-edge arrow pointing toward the active delivery spot.
/// </summary>
public class DeliveryArrowUI : MonoBehaviour
{
    public static DeliveryArrowUI Instance { get; private set; }

    [Header("References")]
    [Tooltip("The arrow Image that will rotate and move to the screen edge.")]
    public RectTransform arrowRect;
    [Tooltip("Main camera reference (leave blank to auto-find).")]
    public Camera mainCam;

    private UnityEngine.UI.Image arrowImage;

    [Header("Settings")]
    [Tooltip("How far from screen center the arrow sits (in pixels).")]
    public float screenEdgeOffset = 80f;

    private Transform target = null;
    private bool isVisible = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Cache the Image component - use enabled flag instead of SetActive
        // so this script keeps running even when the arrow is hidden
        arrowImage = GetComponent<UnityEngine.UI.Image>();

        HideArrow();
    }

    void Start()
    {
        if (mainCam == null)
            mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (!isVisible || target == null || mainCam == null) return;

        Vector3 screenPos = mainCam.WorldToViewportPoint(target.position);

        // Check if target is behind the camera
        bool isBehind = screenPos.z < 0f;

        // Convert viewport (0-1) to determine if on screen
        bool onScreen = !isBehind
                        && screenPos.x > 0.05f && screenPos.x < 0.95f
                        && screenPos.y > 0.05f && screenPos.y < 0.95f;

        if (onScreen)
        {
            // Target is visible — hide the arrow
            if (arrowRect != null) arrowRect.gameObject.SetActive(false);
        }
        else
        {
            // Target is off screen — show and position arrow at screen edge
            if (arrowRect != null) arrowRect.gameObject.SetActive(true);
            PositionArrowAtEdge(screenPos, isBehind);
        }
    }

    void PositionArrowAtEdge(Vector3 screenPos, bool isBehind)
    {
        // If behind camera, flip the direction
        Vector2 dir;
        if (isBehind)
        {
            dir = new Vector2(
                -(screenPos.x - 0.5f),
                -(screenPos.y - 0.5f)
            );
        }
        else
        {
            dir = new Vector2(
                screenPos.x - 0.5f,
                screenPos.y - 0.5f
            );
        }

        dir.Normalize();

        // Convert to screen space and clamp to edge
        float halfW = Screen.width  * 0.5f - screenEdgeOffset;
        float halfH = Screen.height * 0.5f - screenEdgeOffset;

        float scale = Mathf.Min(
            halfW / Mathf.Abs(dir.x),
            halfH / Mathf.Abs(dir.y)
        );

        Vector2 edgePos = dir * scale;

        arrowRect.anchoredPosition = edgePos;

        // Rotate arrow to point toward the target
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowRect.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    public void SetTarget(Transform t)
    {
        target = t;
        isVisible = true;
        if (arrowImage != null) arrowImage.enabled = true;
    }

    public void HideArrow()
    {
        isVisible = false;
        target = null;
        if (arrowImage != null) arrowImage.enabled = false;
    }
}
