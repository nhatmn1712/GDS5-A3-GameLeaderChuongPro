using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class NpcOrderInteract : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The NpcCustomer component on this NPC (will auto-find if empty).")]
    public NpcCustomer customer;
    [Tooltip("The UI Panel that shows the order (giống panel xe hủ tiếu).")]
    public InteractPromptUI promptUI;

    [Header("Dialogue Settings")]
    [Tooltip("Tên hiển thị của NPC trên panel (VD: Customer, Bà Hai, Chú Ba...)")]
    public string npcDisplayName = "Customer";

    [Header("Settings")]
    public string actionHint = "Nhấn F để nhận order";
    [Tooltip("How close the player needs to be to see the UI and interact.")]
    public float detectRange = 2.5f;

    [HideInInspector]
    public string orderText = "F - Nhận Order";
    private string dialogueText = "";

    private bool playerInRange = false;
    private bool isReadyToOrder = false;
    private bool orderConfirmed = false;

    void Start()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = detectRange;

        if (promptUI != null) promptUI.Hide();
        
        if (customer == null) 
            customer = GetComponentInParent<NpcCustomer>();

        if (customer != null)
        {
            string[] availableOrders = { "HuTieu", "HuTieuKhongHanh", "BunBo", "BunBoKhongHanh" };
            customer.desiredItem = availableOrders[Random.Range(0, availableOrders.Length)];
            dialogueText = "I would like 1 bowl of " + GetEnglishName(customer.desiredItem) + " please!";
            orderText = "F";
            actionHint = "Take Order";
        }
    }

    private string GetEnglishName(string code)
    {
        if (code == "HuTieu")           return "Hu Tieu (with scallions)";
        if (code == "HuTieuKhongHanh") return "Hu Tieu (no scallions)";
        if (code == "BunBo")            return "Bun Bo (with scallions)";
        if (code == "BunBoKhongHanh")  return "Bun Bo (no scallions)";
        return code;
    }

    void Update()
    {
        if (customer != null && customer.currentState == NpcCustomer.NpcState.Ordering)
        {
            if (!isReadyToOrder && !orderConfirmed)
            {
                isReadyToOrder = true;

                if (playerInRange && promptUI != null)
                {
                    promptUI.Show(orderText, actionHint);
                }
            }
        }
        else
        {
            orderConfirmed = false;
            isReadyToOrder = false;
        }

        if (isReadyToOrder && playerInRange)
        {
            // Kiểm tra xem dialogue có đang chạy không để tránh spam
            if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                TriggerOrderDialogue();
            }
        }
    }

    void TriggerOrderDialogue()
    {
        isReadyToOrder = false;
        if (promptUI != null) promptUI.Hide();

        DialogueLine line = new DialogueLine();
        line.speakerName = "Customer";
        line.dialogueText = dialogueText;
        line.speakerPortrait = null;

        DialogueLine[] lines = { line };

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(lines, ConfirmOrder);
        }
        else
        {
            ConfirmOrder();
        }
    }

    void ConfirmOrder()
    {
        isReadyToOrder = false;
        orderConfirmed = true;

        PlayerInventory.hasActiveOrder = true;

        if (promptUI != null) promptUI.Hide();

        Debug.Log("[NpcOrder] Order confirmed by player!");
        
        if (customer != null) 
        {
            customer.OnOrderConfirmed();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = true;
            if (isReadyToOrder && promptUI != null)
            {
                promptUI.Show(orderText, actionHint);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = false;
            if (promptUI != null) promptUI.Hide();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
