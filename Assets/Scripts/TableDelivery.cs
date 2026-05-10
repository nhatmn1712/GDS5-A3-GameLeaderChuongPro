using UnityEngine;

/// <summary>
/// Attach to an Empty GameObject at the table position.
/// Starts DISABLED - NpcCustomer.SitDown() will enable it when a customer arrives.
/// Uses OnTriggerStay so it works with the Box Collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TableDelivery : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode deliverKey = KeyCode.E;
    public string requiredItemName = "HuTieu";

    [Header("Bowl Placement")]
    public Transform bowlPlacePoint;        // Empty GO placed on top of the table
    public GameObject placedBowlPrefab;     // Prefab of the bowl to show on table
    public float bowlRemoveDelay = 5f;      // Seconds before the bowl disappears

    [Header("UI (optional)")]
    public InteractPromptUI promptUI;

    [Header("NPC Reaction")]
    [Tooltip("Where the NPC should sit. Drag your Waypoint 5 here.")]
    public Transform chairWaypoint;
    public NpcCustomer linkedNpc;           // Link to the NpcCustomer sitting at this table

    private bool bowlDelivered = false;
    private GameObject spawnedBowl = null;

    private bool playerInRange = false;
    private PlayerInteract currentPlayer = null;

    void OnEnable()
    {
        // Reset state every time this table becomes active (new customer arrives)
        bowlDelivered = false;
        spawnedBowl = null;
        playerInRange = false;
        currentPlayer = null;

        if (promptUI != null)
            promptUI.Hide();
            
        // Ensure the collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnDisable()
    {
        // Clean up bowl if the NPC left before it was removed
        if (spawnedBowl != null)
        {
            Destroy(spawnedBowl);
            spawnedBowl = null;
        }

        if (promptUI != null)
            promptUI.Hide();
            
        playerInRange = false;
        currentPlayer = null;
    }

    void Update()
    {
        if (bowlDelivered || !playerInRange || currentPlayer == null) return;

        // Show/hide UI prompt
        if (promptUI != null)
        {
            if (currentPlayer.IsHoldingItem())
                promptUI.Show("Bàn", "Nhấn E để đặt hủ tiếu");
            else
                promptUI.Hide();
        }

        // Receive delivery input
        if (currentPlayer.IsHoldingItem() && Input.GetKeyDown(deliverKey))
        {
            DeliverBowl();
        }
    }

    void DeliverBowl()
    {
        // Take the item from the player
        currentPlayer.DropItemForDelivery();

        // Spawn bowl on the table
        if (placedBowlPrefab != null)
        {
            Vector3 placePos = bowlPlacePoint != null
                ? bowlPlacePoint.position
                : transform.position + Vector3.up * 0.1f;
            spawnedBowl = Instantiate(placedBowlPrefab, placePos, Quaternion.identity);

            // Schedule bowl removal
            Invoke(nameof(RemoveBowl), bowlRemoveDelay);
        }

        bowlDelivered = true;

        // Award $1 to the player
        MoneyManager.AddMoney(1);

        // Notify NPC they received their food
        if (linkedNpc != null)
            linkedNpc.ReceiveItem(requiredItemName);

        if (promptUI != null)
            promptUI.Hide();

        Debug.Log("[Table] Hủ tiếu đã được đặt lên bàn! Player nhận $1.");
    }

    void RemoveBowl()
    {
        if (spawnedBowl != null)
        {
            Destroy(spawnedBowl);
            spawnedBowl = null;
            Debug.Log("[Table] Tô hủ tiếu đã biến mất.");
        }
    }

    // Use triggers so we rely on the Box Collider size instead of a single center point
    void OnTriggerStay(Collider other)
    {
        if (bowlDelivered || !this.enabled) return;

        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = true;
            currentPlayer = pi;
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = false;
            currentPlayer = null;
            if (promptUI != null) promptUI.Hide();
        }
    }
}
