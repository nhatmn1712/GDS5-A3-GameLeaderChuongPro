using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the Pause Panel (the dark background with the buttons) here.")]
    public GameObject pausePanel;

    [Header("Scene Settings")]
    [Tooltip("The exact name of your Main Menu scene.")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        // Ensure the pause menu is hidden when the gameplay starts
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void Update()
    {
        // Check for the Escape key to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freeze all game physics, animations, and movement

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // Unlock and show the cursor so the player can click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Unfreeze the game

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Lock and hide the cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        Debug.Log("Settings button clicked! (You can create another panel and show it here later)");
    }

    public void QuitToMainMenu()
    {
        // IMPORTANT: Must reset timeScale back to 1 before loading a new scene, 
        // otherwise the Main Menu might be permanently frozen!
        Time.timeScale = 1f; 
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
