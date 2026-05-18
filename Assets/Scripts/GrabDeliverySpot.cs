using UnityEngine;
using TMPro;

/// <summary>
/// Place this on an empty GameObject at each delivery house.
/// The NPC does NOT need to be a child — just drag it into the slot.
/// No InteractCanvas needed — uses a built-in screen prompt.
/// </summary>
public class GrabDeliverySpot : MonoBehaviour
{
    [Header("Spot Info")]
    [Tooltip("Friendly name shown in the order panel (e.g. 'House near the park').")]
    public string locationName = "Delivery House";

    [Header("NPC Reference (NOT a child — just drag any NPC here)")]
    [Tooltip("The NPC model standing outside waiting. Place it near this spot in the scene.")]
    public GameObject waitingNpc;
    [Tooltip("The name of the animation state in the Animator to play while waiting.")]
    public string idleStateName = "Standing Idle";

    [Header("Screen Prompt (auto-created, no setup needed)")]
    [Tooltip("Optional: if you still want to use an InteractPromptUI, drag it here. Otherwise a screen prompt will show automatically.")]
    public InteractPromptUI promptUI;

    [Header("Detection")]
    public float interactRadius = 2.5f;

    // ─── State ──────────────────────────────────────────────────────
    public bool IsActive { get; private set; } = false;
    private string expectedFood = "";
    private bool playerInRange = false;
    private PlayerInteract currentPlayer = null;

    // NPC saved position (WORLD, since NPC is NOT a child)
    private Vector3 originalNpcPos;
    private Quaternion originalNpcRot;

    // ─── Screen Prompt ──────────────────────────────────────────────
    private static GameObject screenPromptObj;
    private static TextMeshProUGUI screenPromptText;
    private static GrabDeliverySpot activePromptOwner;

    void Start()
    {
        if (promptUI != null) promptUI.Hide();
        
        // Find the player once at the start
        currentPlayer = FindObjectOfType<PlayerInteract>(true);

        // Store NPC WORLD position and hide them until an order arrives
        if (waitingNpc != null)
        {
            originalNpcPos = waitingNpc.transform.position;
            originalNpcRot = waitingNpc.transform.rotation;
            waitingNpc.SetActive(false);
        }

        // Create the shared screen-space prompt once
        CreateScreenPrompt();
    }

    /// <summary>Creates a simple screen-space text prompt (shared by all spots).</summary>
    static void CreateScreenPrompt()
    {
        if (screenPromptObj != null) return;

        // Find or create a Screen Space Overlay canvas
        Canvas canvas = null;
        foreach (Canvas c in FindObjectsOfType<Canvas>(true))
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.name == "DeliveryPromptCanvas")
            {
                canvas = c;
                break;
            }
        }

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DeliveryPromptCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        // Create the prompt text
        screenPromptObj = new GameObject("DeliveryPrompt");
        screenPromptObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = screenPromptObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.35f);
        rt.anchorMax = new Vector2(0.5f, 0.35f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(500f, 80f);

        // Background
        UnityEngine.UI.Image bg = screenPromptObj.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(screenPromptObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 5f);
        textRt.offsetMax = new Vector2(-10f, -5f);

        screenPromptText = textObj.AddComponent<TextMeshProUGUI>();
        screenPromptText.text = "";
        screenPromptText.fontSize = 28;
        screenPromptText.alignment = TextAlignmentOptions.Center;
        screenPromptText.color = Color.white;

        screenPromptObj.SetActive(false);
    }

    static void ShowScreenPrompt(string message, GrabDeliverySpot owner)
    {
        activePromptOwner = owner;
        if (screenPromptObj != null)
        {
            screenPromptText.text = message;
            screenPromptObj.SetActive(true);
        }
    }

    static void HideScreenPrompt(GrabDeliverySpot owner)
    {
        // Only hide if we are the one showing it
        if (activePromptOwner != owner) return;
        if (screenPromptObj != null)
            screenPromptObj.SetActive(false);
        activePromptOwner = null;
    }

    // ─────────────────────────────────────────────────────────────────

    /// <summary>Called by GrabOrderManager when this spot is assigned an order.</summary>
    public void Activate(string food)
    {
        IsActive = true;
        expectedFood = food;

        // Show the NPC at the door waiting
        if (waitingNpc != null)
        {
            waitingNpc.SetActive(true);
            
            // Reset to original WORLD position
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
        HideScreenPrompt(this);
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

        // 2. Show prompt and allow interaction if in range AND holding the bowl
        if (playerInRange && currentPlayer.IsHoldingItem())
        {
            ShowScreenPrompt("Press  E  to deliver the order", this);
            if (promptUI != null) promptUI.Show("Press E", "to deliver order");

            if (Input.GetKeyDown(KeyCode.E))
                TryDeliver();
        }
        else
        {
            HideScreenPrompt(this);
            if (promptUI != null) promptUI.Hide();
        }
    }

    void TryDeliver()
    {
        string deliveredFood = PlayerInventory.carryingBowl;

        // Take the food from the player
        currentPlayer.DropItemForDelivery();
        PlayerInventory.carryingBowl = "";

        HideScreenPrompt(this);
        if (promptUI != null) promptUI.Hide();

        if (deliveredFood == expectedFood)
        {
            // Correct!
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
        yield return new WaitForSeconds(0.5f);
        waitingNpc.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.25f);
        Gizmos.DrawSphere(transform.position, interactRadius);
        Gizmos.color = new Color(0f, 1f, 0.3f, 1f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
