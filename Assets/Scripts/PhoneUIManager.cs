using UnityEngine;

public class PhoneUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main container GameObject for the Phone UI (the background image).")]
    public GameObject phonePanel;
    [Tooltip("The TextMeshPro text that displays the current time.")]
    public TMPro.TextMeshProUGUI timeText;

    [Header("Game References")]
    [Tooltip("Reference to the DayNightCycle script to get the time.")]
    public DayNightCycle dayNightCycle;

    private bool isPhoneOpen = false;

    void Start()
    {
        // Ensure the phone is hidden when the game starts
        if (phonePanel != null)
        {
            phonePanel.SetActive(false);
        }
    }

    void Update()
    {
        // Listen for the Tab key to toggle the phone on and off
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePhone();
        }

        // If the phone is open, constantly update the clock!
        if (isPhoneOpen && dayNightCycle != null && timeText != null)
        {
            UpdateTimeDisplay();
        }
    }

    void TogglePhone()
    {
        isPhoneOpen = !isPhoneOpen;

        if (phonePanel != null)
        {
            phonePanel.SetActive(isPhoneOpen);
        }

        if (isPhoneOpen)
        {
            // Unlock the cursor so the player can click on apps (game keeps running!)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Lock the cursor back so the player can control the camera
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void UpdateTimeDisplay()
    {
        // timeOfDay is a float from 0 to 24
        float time = dayNightCycle.timeOfDay;
        
        // Extract hours and minutes
        int hours = Mathf.FloorToInt(time);
        int minutes = Mathf.FloorToInt((time - hours) * 60f);

        // Convert to 12-hour format with AM/PM
        string amPm = hours < 12 ? "AM" : "PM";
        
        int displayHours = hours % 12;
        if (displayHours == 0) displayHours = 12; // Midnight is 12 AM, Noon is 12 PM

        // Format the string nicely (e.g., "9:05 AM")
        timeText.text = string.Format("{0}:{1:00} {2}", displayHours, minutes, amPm);
    }
}
