using UnityEngine;

/// <summary>
/// Singleton that tracks the player's money.
/// In the future, drag a UI Text element into the moneyText slot to display it on screen.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Money")]
    public int playerMoney = 0;

    [Header("UI")]
    [Tooltip("Drag your MoneyText (TextMeshPro) from the Canvas into this slot.")]
    public TMPro.TextMeshProUGUI moneyText;

    void Awake()
    {
        // Singleton setup - only one MoneyManager can exist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    /// <summary>Call this to add money to the player's total.</summary>
    public static void AddMoney(int amount)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[MoneyManager] No MoneyManager found in scene!");
            return;
        }

        Instance.playerMoney += amount;
        Debug.Log($"[MoneyManager] +${amount} earned! Total: ${Instance.playerMoney}");
        Instance.UpdateUI();
    }

    private void UpdateUI()
    {
        if (moneyText != null)
        {
            // Update the text to just the number, since you have a separate $ icon
            moneyText.text = playerMoney.ToString();
        }
    }
}
