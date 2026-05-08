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

    // TODO: In the future, assign a UI Text here to show money on screen (top-right panel)
    // public TMPro.TextMeshProUGUI moneyText;

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

        // TODO: Update UI when you have the money image ready
        // if (Instance.moneyText != null)
        //     Instance.moneyText.text = "$" + Instance.playerMoney;
    }
}
