using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý việc player cầm vật phẩm và giao cho NPC.
/// Tương tác với xe hủ tiếu được xử lý bởi FoodCartInteract (Hold-to-Interact).
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    [Header("Hold Point")]
    public Transform holdPoint; // Điểm cầm nắm (Empty GameObject con của Camera hoặc Player)
    [Tooltip("Điều chỉnh kích thước của vật phẩm khi cầm trên tay (giúp sửa lỗi tô khổng lồ)")]
    public Vector3 holdScale = new Vector3(1f, 1f, 1f);

    [Header("NPC Delivery Settings")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;

    // Trạng thái nội bộ
    private GameObject heldItem = null;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Chỉ dùng phím E để giao cho NPC (hold-to-interact với xe được xử lý bởi FoodCartInteract)
        if (Input.GetKeyDown(interactKey))
        {
            TryDeliverToNPC();
        }
    }

    /// <summary>Kiểm tra và giao vật phẩm cho NPC phía trước mặt.</summary>
    void TryDeliverToNPC()
    {
        if (heldItem == null) return; // Không cầm gì thì không giao được

        Transform rayOrigin = mainCamera != null ? mainCamera.transform : transform;
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            NpcCustomer npc = hit.collider.GetComponent<NpcCustomer>();
            if (npc != null)
            {
                PickupItem heldItemData = heldItem.GetComponent<PickupItem>();
                if (heldItemData != null && npc.ReceiveItem(heldItemData.itemName))
                {
                    Destroy(heldItem);
                    heldItem = null;
                    Debug.Log("[Player] Đã giao hủ tiếu thành công!");
                }
                else
                {
                    Debug.Log("[Player] NPC không cần món này!");
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    // Public API - được gọi bởi FoodCartInteract
    // ──────────────────────────────────────────────────────────

    /// <summary>Trả về true nếu player đang cầm vật phẩm.</summary>
    public bool IsHoldingItem() => heldItem != null;

    /// <summary>Buộc player nhặt một vật phẩm (được gọi bởi FoodCartInteract khi hold xong).</summary>
    public void ForcePickUp(GameObject itemObject)
    {
        if (heldItem != null)
        {
            Debug.Log("[Player] Đang cầm rồi, không thể lấy thêm!");
            Destroy(itemObject); // Hủy vật phẩm thừa
            return;
        }

        heldItem = itemObject;

        // Tắt vật lý
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }

        // Tắt collider
        Collider col = heldItem.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Gắn vào HoldPoint
        if (holdPoint != null)
        {
            heldItem.transform.SetParent(holdPoint);
            heldItem.transform.localPosition = Vector3.zero;
            heldItem.transform.localRotation = Quaternion.identity;
            heldItem.transform.localScale = holdScale; // Áp dụng kích thước thu nhỏ
        }
        else
        {
            heldItem.transform.SetParent(transform);
            heldItem.transform.localPosition = new Vector3(0f, 1f, 1f);
            heldItem.transform.localRotation = Quaternion.identity;
            heldItem.transform.localScale = holdScale; // Áp dụng kích thước thu nhỏ
        }

        Debug.Log("[Player] Đã nhận: " + heldItem.name);
    }

    /// <summary>Thả vật phẩm khi giao cho bàn (không cần vật lý, gọi từ TableDelivery).</summary>
    public void DropItemForDelivery()
    {
        if (heldItem == null) return;
        heldItem.transform.SetParent(null);
        // Ẩn đi trước - TableDelivery sẽ spawn tô mới trên bàn
        Destroy(heldItem);
        heldItem = null;
    }

    /// <summary>Thả vật phẩm đang cầm (có vật lý).</summary>
    public void DropItem()
    {
        if (heldItem == null) return;
        heldItem.transform.SetParent(null);
        Rigidbody rb = heldItem.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; }
        Collider col = heldItem.GetComponent<Collider>();
        if (col != null) col.enabled = true;
        heldItem = null;
    }
}
