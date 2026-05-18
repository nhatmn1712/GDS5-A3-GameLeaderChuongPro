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
    [Tooltip("Mô hình tô Hủ Tiếu KHÔNG HÀNH đã nấu xong")]
    public GameObject completedHuTieuKhongHanhBowl;
    [Tooltip("Mô hình tô Bún Bò KHÔNG HÀNH đã nấu xong")]
    public GameObject completedBunBoKhongHanhBowl;

    [Header("State (Don't touch)")]
    public BowlType currentBowl = BowlType.None;
    public List<string> currentIngredients = new List<string>();
    public bool isBowlCompleted = false;
    public string completedRecipeName = "";

    // --- CÔNG THỨC NẤU ĂN (Recipes) ---
    // Tên nguyên liệu phải khớp với tên bạn điền trong script CookingIngredient
    private List<string> recipeHuTieu = new List<string> { "Shrimp", "TrungCut", "Pork", "HuTieu", "Scallion", "NuocLeoHuTieu" };
    private List<string> recipeHuTieuKhongHanh = new List<string> { "Shrimp", "TrungCut", "Pork", "HuTieu", "NuocLeoHuTieu" };
    
    private List<string> recipeBunBo = new List<string> { "Beef", "Blood Pudding", "Bun", "Scallion", "NuocLeoBunBo" };
    private List<string> recipeBunBoKhongHanh = new List<string> { "Beef", "Blood Pudding", "Bun", "NuocLeoBunBo" };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 1. Tự động xóa EventSystem bị trùng (sửa lỗi không click được khi dùng Additive Load)
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystems.Length > 1)
        {
            foreach (var es in eventSystems)
            {
                if (es.gameObject.scene == this.gameObject.scene)
                {
                    Destroy(es.gameObject);
                }
            }
        }

        // 2. Đảm bảo CookingCamera được bật, là MainCamera để click hoạt động, và xóa AudioListener bị trùng
        CookingCamera[] cams = Resources.FindObjectsOfTypeAll<CookingCamera>();
        if (cams.Length > 0 && cams[0] != null)
        {
            cams[0].gameObject.SetActive(true);
            cams[0].gameObject.tag = "MainCamera"; // Rất quan trọng: OnMouseDown cần có MainCamera!
            AudioListener al = cams[0].GetComponent<AudioListener>();
            if (al != null) Destroy(al);
        }
    }

    void Start()
    {
        ResetCookingStation();
        if (errorPanel != null) errorPanel.SetActive(false);
    }

    void Update()
    {
        // Fallback: Cho phép click chuột phải để hủy tô nhanh nếu click vào bàn bị vướng
        if (Input.GetMouseButtonDown(1)) 
        {
            if (currentBowl != BowlType.None && !isBowlCompleted)
            {
                OnHoldingPlaceClicked(); // Gọi hàm vứt tô
            }
        }
    }

    // Được gọi khi bấm vào chồng tô (để lấy tô mới ra)
    public void OnBowlStackClicked(BowlType type)
    {
        if (currentBowl != BowlType.None)
        {
            ShowError("Please clear the current bowl first!");
            return;
        }

        currentBowl = type;
        currentIngredients.Clear();
        isBowlCompleted = false;
        completedRecipeName = "";
        UpdateVisuals();
    }

    // Được gọi khi bấm vào HoldingPlace (để vứt tô hoặc giao tô)
    public void OnHoldingPlaceClicked()
    {
        if (currentBowl == BowlType.None) return;

        if (isBowlCompleted)
        {
            // Pick up the completed bowl
            PlayerInventory.carryingBowl = completedRecipeName;
            
            // Tìm FoodCartInteract trong scene chính để spawn prefab cho player cầm
            FoodCartInteract cart = FindObjectOfType<FoodCartInteract>();
            if (cart != null)
            {
                cart.SpawnBowlForPlayer(completedRecipeName);
            }

            // Tắt scene nấu ăn
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("MiniGameHuTieu");
        }
        else
        {
            // Discard the incorrect bowl
            ResetCookingStation();
            ShowError("Bowl discarded! Start over.");
        }
    }

    // Được gọi khi bấm vào từng nguyên liệu (Tôm, Thịt bò, Nước lèo...)
    public void OnIngredientClicked(string ingredient)
    {
        // Tự động tắt lỗi cũ nếu người chơi đang thao tác tiếp
        HideError();

        if (currentBowl == BowlType.None)
        {
            ShowError("You must grab an empty bowl first!");
            return;
        }

        if (isBowlCompleted)
        {
            ShowError("This bowl is done! Deliver it to the customer.");
            return;
        }

        if (currentIngredients.Contains(ingredient))
        {
            ShowError("This ingredient is already in the bowl!");
            return;
        }

        // Kiểm tra xem nguyên liệu này có được phép bỏ vào tô hiện tại không
        if (currentBowl == BowlType.WhiteHuTieu && !recipeHuTieu.Contains(ingredient))
        {
            ShowError($"This ingredient doesn't belong in a Hu Tieu bowl!");
            return;
        }
        else if (currentBowl == BowlType.YellowBunBo && !recipeBunBo.Contains(ingredient))
        {
            ShowError($"This ingredient doesn't belong in a Bun Bo bowl!");
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
        if (currentBowl == BowlType.WhiteHuTieu)
        {
            if (IsRecipeMatch(recipeHuTieu)) CompleteBowl("HuTieu");
            else if (IsRecipeMatch(recipeHuTieuKhongHanh)) CompleteBowl("HuTieuKhongHanh");
        }
        else if (currentBowl == BowlType.YellowBunBo)
        {
            if (IsRecipeMatch(recipeBunBo)) CompleteBowl("BunBo");
            else if (IsRecipeMatch(recipeBunBoKhongHanh)) CompleteBowl("BunBoKhongHanh");
        }
    }

    private bool IsRecipeMatch(List<string> recipe)
    {
        // Phải đủ số lượng món
        if (currentIngredients.Count != recipe.Count) return false;

        // Phải chứa đúng các món yêu cầu
        foreach (string req in recipe)
        {
            if (!currentIngredients.Contains(req)) return false;
        }
        return true;
    }

    private void CompleteBowl(string recipeName)
    {
        isBowlCompleted = true;
        completedRecipeName = recipeName;
        UpdateVisuals();
        Debug.Log("Bowl completed: " + recipeName);
    }

    private void UpdateVisuals()
    {
        // Ẩn tất cả đi trước
        if (holdingPlaceEmptyWhiteBowl != null) holdingPlaceEmptyWhiteBowl.SetActive(false);
        if (holdingPlaceEmptyYellowBowl != null) holdingPlaceEmptyYellowBowl.SetActive(false);
        if (completedHuTieuBowl != null) completedHuTieuBowl.SetActive(false);
        if (completedBunBoBowl != null) completedBunBoBowl.SetActive(false);
        if (completedHuTieuKhongHanhBowl != null) completedHuTieuKhongHanhBowl.SetActive(false);
        if (completedBunBoKhongHanhBowl != null) completedBunBoKhongHanhBowl.SetActive(false);

        if (currentBowl == BowlType.None)
        {
            if (bowlContentsPanel != null) bowlContentsPanel.SetActive(false);
            return;
        }

        // Hiện tô tương ứng
        if (isBowlCompleted)
        {
            if (completedRecipeName == "HuTieu" && completedHuTieuBowl != null) completedHuTieuBowl.SetActive(true);
            else if (completedRecipeName == "HuTieuKhongHanh" && completedHuTieuKhongHanhBowl != null) completedHuTieuKhongHanhBowl.SetActive(true);
            else if (completedRecipeName == "BunBo" && completedBunBoBowl != null) completedBunBoBowl.SetActive(true);
            else if (completedRecipeName == "BunBoKhongHanh" && completedBunBoKhongHanhBowl != null) completedBunBoKhongHanhBowl.SetActive(true);
            
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
                string contents = "In bowl: ";
                if (currentIngredients.Count == 0) contents += "Empty";
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
        completedRecipeName = "";
        UpdateVisuals();
    }
}
