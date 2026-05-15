using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemHoverInteract : MonoBehaviour
{
    [Tooltip("Tên món ăn sẽ hiển thị trên UI. Nếu để trống sẽ tự động lấy từ script PickupItem (nếu có).")]
    public string itemName = "";

    private Outline outline;

    void Awake()
    {
        // Tự động lấy tên từ PickupItem nếu itemName đang trống
        if (string.IsNullOrEmpty(itemName))
        {
            PickupItem pickup = GetComponent<PickupItem>();
            if (pickup != null)
            {
                itemName = pickup.itemName;
            }
        }

        // Kiểm tra xem object đã có component Outline chưa, nếu chưa có thì tự động thêm
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = Color.yellow; // Bạn có thể đổi màu ở đây
            outline.OutlineWidth = 5f;
        }
        
        // Luôn tắt outline lúc ban đầu
        outline.enabled = false;
    }

    void OnMouseEnter()
    {
        // Khi chuột trỏ vào: Bật Outline
        if (outline != null) 
        {
            outline.enabled = true;
        }
            
        // Gọi UI Manager để hiện Panel tên món
        if (HoverUIManager.Instance != null && !string.IsNullOrEmpty(itemName))
        {
            HoverUIManager.Instance.ShowPanel(itemName);
        }
    }

    void OnMouseExit()
    {
        // Khi chuột rời khỏi: Tắt Outline
        if (outline != null) 
        {
            outline.enabled = false;
        }
            
        // Gọi UI Manager để ẩn Panel
        if (HoverUIManager.Instance != null)
        {
            HoverUIManager.Instance.HidePanel();
        }
    }
}
