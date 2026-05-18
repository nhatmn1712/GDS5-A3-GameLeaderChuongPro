using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TableDelivery : MonoBehaviour
{
    public enum TableState { WaitingForFood, WrongFood, DeliveryDone }
    public TableState currentState = TableState.WaitingForFood;

    [Header("Settings")]
    public KeyCode deliverKey = KeyCode.E;
    [Tooltip("Scale of the bowl when shown at the pickup counter")]
    public Vector3 tableScale = new Vector3(1f, 1f, 1f);

    [Header("Full Bowl Prefabs (When Delivered)")]
    public GameObject huTieuFull;
    public GameObject huTieuKhongHanhFull;
    public GameObject bunBoFull;
    public GameObject bunBoKhongHanhFull;

    [Header("Placement")]
    public Transform bowlPlacePoint;

    [Header("UI (optional)")]
    public InteractPromptUI promptUI;

    [Header("NPC Reaction")]
    public Transform chairWaypoint;
    public NpcCustomer linkedNpc;

    private GameObject spawnedBowl = null;
    private bool playerInRange = false;
    private PlayerInteract currentPlayer = null;

    void Start()
    {
        this.enabled = false;
    }

    void OnEnable()
    {
        currentState = TableState.WaitingForFood;
        ClearCounter();
        playerInRange = false;
        currentPlayer = null;

        InteractPromptUI activeUI = GetActivePromptUI();
        if (activeUI != null) activeUI.Hide();

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnDisable()
    {
        ClearCounter();
        InteractPromptUI activeUI = GetActivePromptUI();
        if (activeUI != null) activeUI.Hide();
        playerInRange = false;
        currentPlayer = null;
    }

    // Borrow the NPC's floating canvas so prompts appear above their head
    private InteractPromptUI GetActivePromptUI()
    {
        if (linkedNpc != null)
        {
            NpcOrderInteract npcOrder = linkedNpc.GetComponentInChildren<NpcOrderInteract>();
            if (npcOrder != null && npcOrder.promptUI != null)
                return npcOrder.promptUI;
        }
        return promptUI;
    }

    void Update()
    {
        if (!playerInRange || currentPlayer == null || linkedNpc == null) return;

        InteractPromptUI activeUI = GetActivePromptUI();

        if (currentState == TableState.WaitingForFood)
        {
            // Luôn hiện nút E khi khách đang đợi thức ăn (dù player có đang cầm bát hay không)
            if (activeUI != null) activeUI.Show("E", "Khách Hàng", "Giao đồ ăn");

            if (currentPlayer.IsHoldingItem())
            {
                if (Input.GetKeyDown(deliverKey))
                {
                    DeliverFood();
                }
            }
        }
        else if (currentState == TableState.WrongFood)
        {
            // Wrong food - show reminder and let player press E again with correct bowl
            if (currentPlayer.IsHoldingItem())
            {
                if (activeUI != null) activeUI.Show("E", "Khách Hàng", "Đổi món khác");
                if (Input.GetKeyDown(deliverKey))
                {
                    DeliverFood();
                }
            }
        }
        else // DeliveryDone
        {
            if (activeUI != null) activeUI.Hide();
        }
    }

    void DeliverFood()
    {
        string deliveredItem = PlayerInventory.carryingBowl;
        currentPlayer.DropItemForDelivery();
        PlayerInventory.carryingBowl = "";

        if (deliveredItem == linkedNpc.desiredItem)
        {
            // CORRECT food!
            currentState = TableState.DeliveryDone;
            PlayerInventory.hasActiveOrder = false;
            ClearCounter();

            InteractPromptUI activeUI = GetActivePromptUI();
            if (activeUI != null) activeUI.Hide();

            // NPC says thank you then walks away
            DialogueLine line = new DialogueLine();
            line.speakerName = "Customer";
            line.dialogueText = "Thank you so much! This looks delicious!";
            line.speakerPortrait = null;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new DialogueLine[] { line }, () =>
                {
                    MoneyManager.AddMoney(1);
                    linkedNpc.WalkAway();
                });
            }
            else
            {
                MoneyManager.AddMoney(1);
                linkedNpc.WalkAway();
            }
        }
        else
        {
            // WRONG food - give bowl back to player and show NPC dialogue
            currentState = TableState.WrongFood;

            // Give the bowl back visually
            SpawnBowlOnCounter(deliveredItem);

            DialogueLine line = new DialogueLine();
            line.speakerName = "Customer";
            string orderedFoodName = GetFoodDisplayName(linkedNpc.desiredItem);
            line.dialogueText = $"I'm sorry, this is not what I ordered. I ordered {orderedFoodName}. Please check again!";
            line.speakerPortrait = null;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(new DialogueLine[] { line }, () =>
                {
                    // After dialogue closes, clear the counter and reset so player can try again
                    ClearCounter();
                    currentState = TableState.WaitingForFood;

                    // Give hasActiveOrder back so player can go cook again
                    PlayerInventory.hasActiveOrder = true;
                });
            }
            else
            {
                ClearCounter();
                currentState = TableState.WaitingForFood;
                PlayerInventory.hasActiveOrder = true;
            }
        }
    }

    void SpawnBowlOnCounter(string recipeName)
    {
        ClearCounter();
        GameObject prefabToSpawn = null;

        if (recipeName == "HuTieu") prefabToSpawn = huTieuFull;
        else if (recipeName == "HuTieuKhongHanh") prefabToSpawn = huTieuKhongHanhFull;
        else if (recipeName == "BunBo") prefabToSpawn = bunBoFull;
        else if (recipeName == "BunBoKhongHanh") prefabToSpawn = bunBoKhongHanhFull;

        if (prefabToSpawn != null)
        {
            Vector3 placePos = bowlPlacePoint != null ? bowlPlacePoint.position : transform.position + Vector3.up * 0.1f;
            spawnedBowl = Instantiate(prefabToSpawn, placePos, Quaternion.identity);

            if (bowlPlacePoint != null) spawnedBowl.transform.SetParent(bowlPlacePoint);
            else spawnedBowl.transform.SetParent(transform);

            spawnedBowl.transform.localScale = tableScale;

            // Disable colliders so the bowl doesn't push the player
            foreach (Collider c in spawnedBowl.GetComponentsInChildren<Collider>())
                c.enabled = false;
        }
    }

    void ClearCounter()
    {
        if (spawnedBowl != null)
        {
            Destroy(spawnedBowl);
            spawnedBowl = null;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!this.enabled) return;

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
            InteractPromptUI activeUI = GetActivePromptUI();
            if (activeUI != null) activeUI.Hide();
        }
    }

    private string GetFoodDisplayName(string internalName)
    {
        switch (internalName)
        {
            case "HuTieu": return "Hu Tieu (with scallions)";
            case "HuTieuKhongHanh": return "Hu Tieu (no scallions)";
            case "BunBo": return "Bun Bo (with scallions)";
            case "BunBoKhongHanh": return "Bun Bo (no scallions)";
            default: return internalName;
        }
    }
}
