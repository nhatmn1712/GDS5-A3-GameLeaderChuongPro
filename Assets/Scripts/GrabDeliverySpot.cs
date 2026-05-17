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
    [Tooltip("The name of the animation state in the Animator to play while waiting.")]
    public string idleStateName = "Standing Idle";

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

    private Vector3 originalNpcPos;
    private Quaternion originalNpcRot;

    void Start()
    {
        if (promptUI != null) promptUI.Hide();
        
        // Find the player once at the start
        currentPlayer = FindObjectOfType<PlayerInteract>(true);

        // Store original NPC position and hide them until an order arrives
        if (waitingNpc != null)
        {
            originalNpcPos = waitingNpc.transform.position;
            originalNpcRot = waitingNpc.transform.rotation;
            waitingNpc.SetActive(false); // Hide initially
        }
    }

    /// <summary>Called by GrabOrderManager when this spot is assigned an order.</summary>
    public void Activate(string food)
    {
        IsActive = true;
        expectedFood = food;
        if (promptUI != null) promptUI.Hide();

        // Show the NPC at the door waiting
        if (waitingNpc != null)
        {
            waitingNpc.SetActive(true);
            
            // Reset position and rotation
            waitingNpc.transform.position = originalNpcPos;
            waitingNpc.transform.rotation = originalNpcRot;

            Animator anim = waitingNpc.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool("IsMoving", false);
                anim.Play(idleStateName);
            }
        }

        Debug.Log($"[GrabSpot] {locationName} is now active, expecting: {food}");
    }

    /// <summary>Called by GrabOrderManager when the delivery is completed or cancelled.</summary>
    public void Deactivate()
    {
        IsActive = false;
        expectedFood = "";
        playerInRange = false;
        if (promptUI != null) promptUI.Hide();
    }

    void Update()
    {
        if (!IsActive) return;

        if (currentPlayer == null) 
            currentPlayer = FindObjectOfType<PlayerInteract>(true);
            
        if (currentPlayer == null) return;

        // 1. Check Distance (handles both walking and riding the bike)
        playerInRange = false;
        
        if (currentPlayer.gameObject.activeInHierarchy)
        {
            // Player is walking
            if (Vector3.Distance(transform.position, currentPlayer.transform.position) <= interactRadius)
                playerInRange = true;
        }
        else
        {
            // Player might be hidden because they are on the bike
            BikeController bike = FindObjectOfType<BikeController>();
            if (bike != null && bike.gameObject.activeInHierarchy)
            {
                if (Vector3.Distance(transform.position, bike.transform.position) <= interactRadius)
                    playerInRange = true;
            }
        }

        // 2. Show UI and allow interaction if in range AND holding the bowl
        if (playerInRange && currentPlayer.IsHoldingItem())
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
                    StartCoroutine(NpcWalkAwayRoutine());
                });
            }
            else
            {
                GrabOrderManager.Instance?.CompleteDelivery();
                StartCoroutine(NpcWalkAwayRoutine());
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

    System.Collections.IEnumerator NpcWalkAwayRoutine()
    {
        if (waitingNpc == null) yield break;

        // Wait a tiny fraction of a second after dialogue finishes
        yield return new WaitForSeconds(0.5f);

        // Magically disappear until the next order!
        waitingNpc.SetActive(false);
    }

    // Removed OnTriggerEnter and OnTriggerExit because we are now using reliable distance checking in Update() instead.

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.25f);
        Gizmos.DrawSphere(transform.position, interactRadius);
        Gizmos.color = new Color(0f, 1f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
