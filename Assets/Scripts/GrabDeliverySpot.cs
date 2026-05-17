using UnityEngine;

/// <summary>
/// Place this on an empty GameObject at each delivery house.
/// Handles the player approaching and pressing E to deliver.
/// </summary>
public class GrabDeliverySpot : MonoBehaviour
{
    [Header("Spot Info")]
    [Tooltip("Friendly name shown in the order panel (e.g. 'House near the park').")]
    public string locationName = "Delivery House";

    [Header("NPC Reference")]
    [Tooltip("The NPC model standing outside waiting. Can be any GameObject.")]
    public GameObject waitingNpc;

    [Header("UI")]
    [Tooltip("Drag the InteractCanvas here so the prompt appears above the NPC.")]
    public InteractPromptUI promptUI;

    [Header("Detection")]
    public float interactRadius = 2.5f;

    // ─── State ──────────────────────────────────────────────────────
    public bool IsActive { get; private set; } = false;
    private string expectedFood = "";
    private bool playerInRange = false;
    private PlayerInteract currentPlayer = null;

    void Start()
    {
        // NPC always visible, but spot is inactive until an order comes in
        if (promptUI != null) promptUI.Hide();
    }

    /// <summary>Called by GrabOrderManager when this spot is assigned an order.</summary>
    public void Activate(string food)
    {
        IsActive = true;
        expectedFood = food;
        if (promptUI != null) promptUI.Hide(); // Will show when player enters range
        Debug.Log($"[GrabSpot] {locationName} is now active, expecting: {food}");
    }

    /// <summary>Called by GrabOrderManager when the delivery is completed or cancelled.</summary>
    public void Deactivate()
    {
        IsActive = false;
        expectedFood = "";
        playerInRange = false;
        currentPlayer = null;
        if (promptUI != null) promptUI.Hide();
    }

    void Update()
    {
        if (!IsActive || !playerInRange || currentPlayer == null) return;

        if (currentPlayer.IsHoldingItem())
        {
            if (promptUI != null) promptUI.Show("Press E", "to deliver order");

            if (Input.GetKeyDown(KeyCode.E))
                TryDeliver();
        }
        else
        {
            if (promptUI != null) promptUI.Hide();
        }
    }

    void TryDeliver()
    {
        string deliveredFood = PlayerInventory.carryingBowl;

        // Take the food from the player
        currentPlayer.DropItemForDelivery();
        PlayerInventory.carryingBowl = "";

        if (deliveredFood == expectedFood)
        {
            // Correct!
            if (promptUI != null) promptUI.Hide();

            DialogueLine line = new DialogueLine();
            line.speakerName = "Customer";
            line.dialogueText = "Thank you so much! This is exactly what I ordered!";
            line.speakerPortrait = null;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new DialogueLine[] { line }, () =>
                {
                    GrabOrderManager.Instance?.CompleteDelivery();
                });
            }
            else
            {
                GrabOrderManager.Instance?.CompleteDelivery();
            }
        }
        else
        {
            // Wrong food!
            DialogueLine line = new DialogueLine();
            line.speakerName = "Customer";
            line.dialogueText = "I'm sorry, this is not what I ordered. Please check again!";
            line.speakerPortrait = null;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new DialogueLine[] { line }, () =>
                {
                    GrabOrderManager.Instance?.WrongFoodDelivered();
                    // Give back the active order so player can go cook again
                    PlayerInventory.hasActiveOrder = true;
                });
            }
            else
            {
                GrabOrderManager.Instance?.WrongFoodDelivered();
                PlayerInventory.hasActiveOrder = true;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;
        PlayerInteract pi = other.GetComponent<PlayerInteract>() ?? other.GetComponentInParent<PlayerInteract>();
        if (pi != null)
        {
            playerInRange = true;
            currentPlayer = pi;
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInteract pi = other.GetComponent<PlayerInteract>() ?? other.GetComponentInParent<PlayerInteract>();
        if (pi != null)
        {
            playerInRange = false;
            currentPlayer = null;
            if (promptUI != null) promptUI.Hide();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.25f);
        Gizmos.DrawSphere(transform.position, interactRadius);
        Gizmos.color = new Color(0f, 1f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
