using UnityEngine;

/// <summary>
/// Gắn vào một Empty GameObject đặt tại mặt bàn.
/// Khi player đến gần và nhấn E (đang cầm tô), tô sẽ được đặt lên bàn.
/// </summary>
public class TableDelivery : MonoBehaviour
{
    [Header("Settings")]
    public float detectRange = 2f;
    public KeyCode deliverKey = KeyCode.E;
    public string requiredItemName = "HuTieu";

    [Header("Bowl Placement")]
    public Transform bowlPlacePoint;   // Điểm đặt tô trên bàn (đặt Empty GO tại đây)
    public GameObject placedBowlPrefab; // Prefab tô để hiển thị trên bàn sau khi đặt

    [Header("UI (optional)")]
    public InteractPromptUI promptUI;  // Có thể gán cùng canvas hoặc riêng

    [Header("NPC Reaction")]
    public NpcCustomer linkedNpc;      // NPC sẽ phản ứng khi nhận được tô

    private PlayerInteract playerInteract;
    private bool bowlDelivered = false;

    void Start()
    {
        // Tìm player tự động
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            playerInteract = playerGO.GetComponent<PlayerInteract>();

        if (promptUI != null)
            promptUI.Hide();
    }

    void Update()
    {
        if (bowlDelivered) return;
        if (playerInteract == null) return;

        float dist = Vector3.Distance(transform.position, playerInteract.transform.position);
        bool inRange = dist <= detectRange;

        // Hiện/ẩn UI prompt
        if (promptUI != null)
        {
            if (inRange && playerInteract.IsHoldingItem())
                promptUI.Show("Bàn", "Nhấn E để đặt hủ tiếu");
            else
                promptUI.Hide();
        }

        // Nhận input
        if (inRange && playerInteract.IsHoldingItem() && Input.GetKeyDown(deliverKey))
        {
            DeliverBowl();
        }
    }

    void DeliverBowl()
    {
        // Bảo player thả tô
        playerInteract.DropItemForDelivery();

        // Spawn tô trên bàn (nếu có prefab)
        if (placedBowlPrefab != null)
        {
            Vector3 placePos = bowlPlacePoint != null ? bowlPlacePoint.position : transform.position + Vector3.up * 0.1f;
            Instantiate(placedBowlPrefab, placePos, Quaternion.identity);
        }

        bowlDelivered = true;

        // Thông báo NPC
        if (linkedNpc != null)
            linkedNpc.ReceiveItem(requiredItemName);

        if (promptUI != null)
            promptUI.Hide();

        Debug.Log("[Table] Đã đặt hủ tiếu lên bàn!");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
