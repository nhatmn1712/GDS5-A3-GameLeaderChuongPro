using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum BowlType { None, WhiteHuTieu, YellowBunBo }

public class CookingManager : MonoBehaviour
{
    public static CookingManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel UI hiện danh sách nguyên liệu đang có trong tô")]
    public GameObject bowlContentsPanel;
    public TextMeshProUGUI bowlContentsText;
    
    [Tooltip("Panel UI hiện thông báo lỗi (ví dụ: Sai nguyên liệu)")]
    public GameObject errorPanel;
    public TextMeshProUGUI errorText;

    [Header("Visual References")]
    [Tooltip("Mô hình tô trắng rỗng nằm ở HoldingPlace")]
    public GameObject holdingPlaceEmptyWhiteBowl;
    [Tooltip("Mô hình tô vàng rỗng nằm ở HoldingPlace")]
    public GameObject holdingPlaceEmptyYellowBowl;
    [Tooltip("Mô hình tô Hủ Tiếu đã nấu xong (có đầy đủ đồ ăn)")]
    public GameObject completedHuTieuBowl;
    [Tooltip("Mô hình tô Bún Bò đã nấu xong (có đầy đủ đồ ăn)")]
    public GameObject completedBunBoBowl;

    [Header("State (Don't touch)")]
    public BowlType currentBowl = BowlType.None;
    public List<string> currentIngredients = new List<string>();
    public bool isBowlCompleted = false;

    // --- CÔNG THỨC NẤU ĂN (Recipes) ---
    // Tên nguyên liệu phải khớp với tên bạn điền trong script CookingIngredient
    private List<string> recipeHuTieu = new List<string> { "Tom", "TrungCut", "Pork", "HuTieu", "Hanh", "NuocLeoHuTieu" };
    private List<string> recipeBunBo = new List<string> { "Beef", "Huyet", "Bun", "Hanh", "NuocLeoBunBo" };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ResetCookingStation();
        if (errorPanel != null) errorPanel.SetActive(false);
    }

    // Được gọi khi bấm vào chồng tô (để lấy tô mới ra)
    public void OnBowlStackClicked(BowlType type)
    {
        if (currentBowl != BowlType.None)
        {
            ShowError("Vui lòng dọn tô hiện tại ra trước!");
            return;
        }

        currentBowl = type;
        currentIngredients.Clear();
        isBowlCompleted = false;
        UpdateVisuals();
    }

    // Được gọi khi bấm vào HoldingPlace (để vứt tô hoặc giao tô)
    public void OnHoldingPlaceClicked()
    {
        if (currentBowl == BowlType.None) return;

        if (isBowlCompleted)
        {
            // TODO: Phần này sẽ dùng để giao cho NPC sau này
            Debug.Log("Giao tô cho khách!");
            ShowError("Đã giao món ăn!"); // Tạm thời hiện thông báo
            ResetCookingStation(); 
        }
        else
        {
            // Vứt tô làm sai
            ResetCookingStation();
            ShowError("Đã đổ bỏ tô bị hư!");
        }
    }

    // Được gọi khi bấm vào từng nguyên liệu (Tôm, Thịt bò, Nước lèo...)
    public void OnIngredientClicked(string ingredient)
    {
        if (currentBowl == BowlType.None)
        {
            ShowError("Bạn phải lấy tô rỗng ra trước!");
            return;
        }

        if (isBowlCompleted)
        {
            ShowError("Tô này đã nấu xong, hãy giao cho khách!");
            return;
        }

        if (currentIngredients.Contains(ingredient))
        {
            ShowError("Nguyên liệu này đã có trong tô rồi!");
            return;
        }

        // Kiểm tra xem nguyên liệu này có được phép bỏ vào tô hiện tại không
        if (currentBowl == BowlType.WhiteHuTieu && !recipeHuTieu.Contains(ingredient))
        {
            ShowError($"Không thể cho món này vào tô Hủ Tiếu!");
            return;
        }
        else if (currentBowl == BowlType.YellowBunBo && !recipeBunBo.Contains(ingredient))
        {
            ShowError($"Không thể cho món này vào tô Bún Bò!");
            return;
        }

        // Bỏ nguyên liệu vào tô thành công
        currentIngredients.Add(ingredient);
        UpdateVisuals();

        // Kiểm tra xem đã đủ nguyên liệu chưa sau MỖI LẦN thêm món
        CheckRecipeCompletion();
    }

    private void CheckRecipeCompletion()
    {
        List<string> targetRecipe = currentBowl == BowlType.WhiteHuTieu ? recipeHuTieu : recipeBunBo;

        // Nếu chưa bỏ đủ số lượng món thì cứ tiếp tục nấu, không báo lỗi
        if (currentIngredients.Count < targetRecipe.Count)
        {
            return; 
        }

        // Nếu đã bỏ đủ số món, kiểm tra xem có đúng hết không
        bool isCorrect = true;
        foreach (string req in targetRecipe)
        {
            if (!currentIngredients.Contains(req))
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            isBowlCompleted = true;
            UpdateVisuals();
            Debug.Log("Nấu xong một tô hoàn hảo!");
        }
    }

    private void UpdateVisuals()
    {
        // Ẩn tất cả đi trước
        if (holdingPlaceEmptyWhiteBowl != null) holdingPlaceEmptyWhiteBowl.SetActive(false);
        if (holdingPlaceEmptyYellowBowl != null) holdingPlaceEmptyYellowBowl.SetActive(false);
        if (completedHuTieuBowl != null) completedHuTieuBowl.SetActive(false);
        if (completedBunBoBowl != null) completedBunBoBowl.SetActive(false);

        if (currentBowl == BowlType.None)
        {
            if (bowlContentsPanel != null) bowlContentsPanel.SetActive(false);
            return;
        }

        // Hiện tô tương ứng
        if (isBowlCompleted)
        {
            if (currentBowl == BowlType.WhiteHuTieu && completedHuTieuBowl != null) completedHuTieuBowl.SetActive(true);
            else if (currentBowl == BowlType.YellowBunBo && completedBunBoBowl != null) completedBunBoBowl.SetActive(true);
            
            if (bowlContentsPanel != null) bowlContentsPanel.SetActive(false);
        }
        else // Tô đang nấu dở
        {
            if (currentBowl == BowlType.WhiteHuTieu && holdingPlaceEmptyWhiteBowl != null) holdingPlaceEmptyWhiteBowl.SetActive(true);
            else if (currentBowl == BowlType.YellowBunBo && holdingPlaceEmptyYellowBowl != null) holdingPlaceEmptyYellowBowl.SetActive(true);

            // Cập nhật giao diện chữ
            if (bowlContentsPanel != null)
            {
                bowlContentsPanel.SetActive(true);
                string contents = "Trong tô: ";
                if (currentIngredients.Count == 0) contents += "Trống";
                else contents += string.Join(", ", currentIngredients);
                if (bowlContentsText != null) bowlContentsText.text = contents;
            }
        }
    }

    private void ShowError(string msg)
    {
        if (errorText != null) errorText.text = msg;
        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
            CancelInvoke("HideError");
            Invoke("HideError", 3f); // Tự động ẩn lỗi sau 3 giây
        }
    }

    private void HideError()
    {
        if (errorPanel != null) errorPanel.SetActive(false);
    }

    private void ResetCookingStation()
    {
        currentBowl = BowlType.None;
        currentIngredients.Clear();
        isBowlCompleted = false;
        UpdateVisuals();
    }
}
