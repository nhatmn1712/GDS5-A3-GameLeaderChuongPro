using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PhoneUIManager : MonoBehaviour
{
    public static PhoneUIManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The main container GameObject for the Phone UI (the background image).")]
    public GameObject phonePanel;
    [Tooltip("The TextMeshPro text that displays the current time.")]
    public TextMeshProUGUI timeText;

    [Header("Game References")]
    [Tooltip("Reference to the DayNightCycle script to get the time.")]
    public DayNightCycle dayNightCycle;

    [Header("Grab App")]
    [Tooltip("The Grab app icon RectTransform (for the shake animation).")]
    public RectTransform grabAppIcon;
    [Tooltip("The Order Panel that slides in when the player clicks the Grab app.")]
    public GameObject grabOrderPanel;
    [Tooltip("Shows what food the customer ordered.")]
    public TextMeshProUGUI orderFoodText;
    [Tooltip("Shows the delivery location name.")]
    public TextMeshProUGUI orderLocationText;
    [Tooltip("Shows the estimated pay.")]
    public TextMeshProUGUI orderPayText;

    [Header("Buttons")]
    [Tooltip("Drag the Accept Button GameObject here.")]
    public GameObject acceptButton;
    [Tooltip("Drag the Reject Button GameObject here.")]
    public GameObject rejectButton;

    private bool isPhoneOpen = false;
    private bool isShaking = false;
    private Coroutine shakeCoroutine;
    private Vector2 grabIconOriginalPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (phonePanel != null) phonePanel.SetActive(false);
        if (grabOrderPanel != null) grabOrderPanel.SetActive(false);
        if (grabAppIcon != null) grabIconOriginalPos = grabAppIcon.anchoredPosition;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            TogglePhone();

        if (isPhoneOpen && dayNightCycle != null && timeText != null)
            UpdateTimeDisplay();
    }

    public void TogglePhone()
    {
        isPhoneOpen = !isPhoneOpen;

        if (phonePanel != null) phonePanel.SetActive(isPhoneOpen);

        // If closing, also close the order panel
        if (!isPhoneOpen && grabOrderPanel != null)
            grabOrderPanel.SetActive(false);

        if (isPhoneOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Do not lock the cursor if the player is currently in the cooking minigame
            bool isCooking = UnityEngine.SceneManagement.SceneManager.GetSceneByName("MiniGameHuTieu").isLoaded;
            if (!isCooking)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    // ─── Called by GrabOrderManager when a new order arrives ────────
    public void OnGrabNotificationReceived()
    {
        // Auto-open the phone so the player sees the notification
        if (!isPhoneOpen)
        {
            isPhoneOpen = true;
            if (phonePanel != null) phonePanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Start shaking the Grab icon
        if (!isShaking)
        {
            isShaking = true;
            shakeCoroutine = StartCoroutine(ShakeGrabIcon());
        }
    }

    IEnumerator ShakeGrabIcon()
    {
        float elapsed = 0f;
        float duration = 999f; // shake until stopped
        float speed = 20f;
        float amount = 3f; // Subtle shake - easy to click

        while (isShaking)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed * speed) * amount;
            if (grabAppIcon != null)
                grabAppIcon.anchoredPosition = grabIconOriginalPos + new Vector2(x, 0f);
            yield return null;
        }

        // Reset position
        if (grabAppIcon != null)
            grabAppIcon.anchoredPosition = grabIconOriginalPos;
    }

    void StopShake()
    {
        isShaking = false;
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }
        if (grabAppIcon != null)
            grabAppIcon.anchoredPosition = grabIconOriginalPos;
    }

    // ─── Called when player clicks the Grab app icon (wire to Button) ─
    public void OnGrabAppClicked()
    {
        GrabOrderManager gom = GrabOrderManager.Instance;
        if (gom == null) return;

        if (gom.HasPendingNotification)
        {
            StopShake();
            ShowOrderPanel(gom, false); // false = not a reminder, it's a new order
        }
        else if (gom.HasActiveOrder)
        {
            // Show reminder of current active order
            ShowOrderPanel(gom, true); // true = it IS a reminder, hide the buttons!
        }
    }

    void ShowOrderPanel(GrabOrderManager gom, bool isReminder)
    {
        if (grabOrderPanel == null) return;
        grabOrderPanel.SetActive(true);

        if (orderFoodText != null)
            orderFoodText.text = GrabOrderManager.GetFoodDisplayName(gom.OrderedFood);

        if (orderLocationText != null)
            orderLocationText.text = "Deliver to: " + gom.LocationName;

        if (orderPayText != null)
            orderPayText.text = "Estimated Pay: $" + gom.EstimatedPay;

        // Hide buttons if this is just a reminder!
        if (acceptButton != null) acceptButton.SetActive(!isReminder);
        if (rejectButton != null) rejectButton.SetActive(!isReminder);
    }

    // ─── Accept / Reject buttons ─────────────────────────────────────
    public void OnAcceptClicked()
    {
        GrabOrderManager.Instance?.AcceptOrder();
        if (grabOrderPanel != null) grabOrderPanel.SetActive(false);
        TogglePhone(); // Close phone so player can go cook
    }

    public void OnRejectClicked()
    {
        GrabOrderManager.Instance?.RejectOrder();
        if (grabOrderPanel != null) grabOrderPanel.SetActive(false);
    }

    // ─── Clock display ───────────────────────────────────────────────
    void UpdateTimeDisplay()
    {
        float time = dayNightCycle.timeOfDay;
        int hours = Mathf.FloorToInt(time);
        int minutes = Mathf.FloorToInt((time - hours) * 60f);
        string amPm = hours < 12 ? "AM" : "PM";
        int displayHours = hours % 12;
        if (displayHours == 0) displayHours = 12;
        timeText.text = string.Format("{0}:{1:00} {2}", displayHours, minutes, amPm);
    }
}
