using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HoverUIManager : MonoBehaviour
{
    public static HoverUIManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Kéo GameObject Panel chứa đoạn Text vào đây")]
    public GameObject hoverPanel;
    [Tooltip("Kéo UI Text (TMP) dùng để hiển thị tên món vào đây")]
    public TextMeshProUGUI itemNameText;

    [Header("Settings")]
    [Tooltip("Khoảng cách Offset so với con trỏ chuột")]
    public Vector2 offset = new Vector2(20f, -20f);

    void Awake()
    {
        // Singleton pattern để dễ dàng gọi từ các script khác
        if (Instance == null) 
        { 
            Instance = this; 
        }
        else 
        { 
            Destroy(gameObject); 
            return;
        }

        // Tắt Panel lúc bắt đầu game
        if (hoverPanel != null)
        {
            hoverPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Liên tục cập nhật vị trí panel đi theo chuột nếu panel đang được bật
        if (hoverPanel != null && hoverPanel.activeSelf)
        {
            UpdatePanelPosition();
        }
    }

    public void ShowPanel(string name)
    {
        if (hoverPanel == null) return;

        // Cập nhật text
        if (itemNameText != null) 
        {
            itemNameText.text = name;
        }

        // Bật panel và update vị trí ngay lập tức để tránh panel bị chớp ở vị trí cũ
        hoverPanel.SetActive(true);
        UpdatePanelPosition();
    }

    public void HidePanel()
    {
        if (hoverPanel != null)
        {
            hoverPanel.SetActive(false);
        }
    }

    private void UpdatePanelPosition()
    {
        // Lấy vị trí chuột trên màn hình
        Vector2 mousePos = Input.mousePosition;
        
        // Gán vị trí mới cho Panel cộng thêm khoảng offset
        hoverPanel.transform.position = mousePos + offset;
    }
}
