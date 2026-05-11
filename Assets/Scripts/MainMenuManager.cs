using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the Continue Button GameObject here so we can hide it if no save exists.")]
    public GameObject continueButton;

    [Header("Scene Settings")]
    [Tooltip("The exact name of your main gameplay scene.")]
    public string gameplaySceneName = "Testing";

    void Start()
    {
        // Unlock the mouse cursor for the main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Check if there is a save file (if they have saved money).
        if (PlayerPrefs.HasKey("SavedMoney"))
        {
            continueButton.SetActive(true); // Show Continue button
        }
        else
        {
            continueButton.SetActive(false); // Hide Continue button
        }
    }

    /// <summary>
    /// Called by the "Continue" button. Loads the game with existing save data.
    /// </summary>
    public void ContinueGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Called by the "New Game" button. Deletes old save data and starts fresh.
    /// </summary>
    public void NewGame()
    {
        // Delete the saved money to start fresh
        PlayerPrefs.DeleteKey("SavedMoney");
        PlayerPrefs.Save();

        // Load the game scene
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// <summary>
    /// Called by the "Quit" button.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}
