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
    [Tooltip("Tên hiển thị của NPC trên panel (VD: Khách, Bà Hai, Chú Ba...)")]
    public string npcDisplayName = "Khách";

    [Header("Settings")]
    public string actionHint = "Nhấn F để nhận order";
    [Tooltip("How close the player needs to be to see the UI and interact.")]
    public float detectRange = 2.5f;

    [HideInInspector]
    public string orderText = ""; // Sẽ tự động tạo dựa trên món ăn

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
            orderText = GetVietnameseName(customer.desiredItem);
        }
    }

    private string GetVietnameseName(string code)
    {
        if (code == "HuTieu")           return "1 tô Hủ Tiếu (có hành)";
        if (code == "HuTieuKhongHanh") return "1 tô Hủ Tiếu (không hành)";
        if (code == "BunBo")            return "1 tô Bún Bò (có hành)";
        if (code == "BunBoKhongHanh")  return "1 tô Bún Bò (không hành)";
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
                    // Hiện panel giống xe hủ tiếu:
                    // - Dòng trên (title): Tên NPC
                    // - Dòng dưới (action): Món + hint nhấn phím
                    promptUI.Show(npcDisplayName, orderText + "\n" + actionHint);
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
            if (Input.GetKeyDown(KeyCode.F))
            {
                ConfirmOrder();
            }
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
                promptUI.Show(npcDisplayName, orderText + "\n" + actionHint);
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
