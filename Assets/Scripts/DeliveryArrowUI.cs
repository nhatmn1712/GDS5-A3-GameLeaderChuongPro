using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GTA/GPS-style screen-edge delivery indicator.
///
/// Behaviour:
///  • Target ON-screen  → indicator hidden.
///  • Target OFF-screen → badge appears at the nearest screen edge,
///    rotated so its pointed tip aims at the target, and shows live distance.
///
/// Hierarchy (set up via MCP or manually):
///   [DeliveryIndicator]          ← this script + RectTransform
///         └─ [Badge]             ← the visible panel (Image background)
///               ├─ [ArrowTip]   ← small triangle Image pointing "down" in badge-local space
///               └─ [DistText]   ← TextMeshProUGUI showing "XXm"
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class DeliveryArrowUI : MonoBehaviour
{
    public static DeliveryArrowUI Instance { get; private set; }

    // ── References ───────────────────────────────────────────────────────────
    [Header("References")]
    public RectTransform badge;          // The badge RectTransform that rotates + moves
    public TextMeshProUGUI distanceText; // "XXm" text inside the badge
    public Camera mainCam;

    // ── Screen Edge Settings ─────────────────────────────────────────────────
    [Header("Screen Edge")]
    [Tooltip("How far in from the screen border the badge center sits (pixels).")]
    public float edgeMargin = 64f;

    // ── Colours ───────────────────────────────────────────────────────────────
    [Header("Colours")]
    public Color badgeColor    = new Color(0.18f, 0.80f, 0.44f, 1f); // vivid green
    public Color textColor     = Color.white;

    // ── Private ───────────────────────────────────────────────────────────────
    private Transform     target    = null;
    private bool          isActive  = false;
    private RectTransform myRect;
    private Image         badgeImage;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        myRect     = GetComponent<RectTransform>();
        badgeImage = badge != null ? badge.GetComponent<Image>() : null;
        SetVisible(false);
    }

    void Start()
    {
        if (mainCam == null) mainCam = Camera.main;

        // Anchor to screen center so anchoredPosition is in screen-pixel coords
        myRect.anchorMin = new Vector2(0.5f, 0.5f);
        myRect.anchorMax = new Vector2(0.5f, 0.5f);
        myRect.pivot     = new Vector2(0.5f, 0.5f);

        // Apply colours
        if (badgeImage   != null) badgeImage.color   = badgeColor;
        if (distanceText != null) distanceText.color  = textColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (!isActive || target == null || mainCam == null) return;

        // ── Viewport position of target ──────────────────────────────────────
        Vector3 vp = mainCam.WorldToViewportPoint(target.position);
        bool behind = vp.z < 0f;

        // On-screen check (with small margin)
        bool onScreen = !behind
                     && vp.x > 0.05f && vp.x < 0.95f
                     && vp.y > 0.05f && vp.y < 0.95f;

        if (onScreen)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        // ── Direction from screen center → target ────────────────────────────
        Vector2 dir;
        if (behind)
            dir = new Vector2(-(vp.x - 0.5f), -(vp.y - 0.5f));
        else
            dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
        dir.Normalize();

        // ── Clamp to screen edge ─────────────────────────────────────────────
        float halfW = Screen.width  * 0.5f - edgeMargin;
        float halfH = Screen.height * 0.5f - edgeMargin;
        float scale = Mathf.Min(halfW / Mathf.Abs(dir.x + 0.0001f),
                                halfH / Mathf.Abs(dir.y + 0.0001f));

        myRect.anchoredPosition = dir * scale;

        // ── Rotate badge so its tip points toward target ─────────────────────
        if (badge != null)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // Badge tip points UP in local space → offset -90°
            badge.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        // ── Distance label ───────────────────────────────────────────────────
        if (distanceText != null)
        {
            float dist = Vector3.Distance(mainCam.transform.position, target.position);
            distanceText.text = $"{Mathf.RoundToInt(dist)}m";
            // Counter-rotate text so it stays readable
            distanceText.rectTransform.localRotation =
                badge != null
                    ? Quaternion.Inverse(badge.localRotation)
                    : Quaternion.identity;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void SetVisible(bool show)
    {
        if (badge != null) badge.gameObject.SetActive(show);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Call from GrabOrderManager.AcceptOrder().</summary>
    public void SetTarget(Transform t)
    {
        target   = t;
        isActive = true;
    }

    /// <summary>Call from GrabOrderManager.CompleteDelivery() / RejectOrder().</summary>
    public void HideArrow()
    {
        isActive = false;
        target   = null;
        SetVisible(false);
    }
}
