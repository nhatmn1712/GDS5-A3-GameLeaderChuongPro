using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gắn vào World Space Canvas trên xe hủ tiếu.
/// Xử lý Fade In/Out mượt và Billboard (luôn xoay về camera).
/// </summary>
public class InteractPromptUI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;      // CanvasGroup trên Panel gốc
    public Image loadingArcImage;        // Image kiểu Filled Radial360 - vòng loading
    public Text keyText;                // Text hiển thị phím bấm (ví dụ: E, F)
    public Text itemNameText;           // Text tên vật thể / Tiêu đề
    public Text actionText;             // Text hướng dẫn hành động

    [Header("Fade Settings")]
    public float fadeSpeed = 4f;         // Tốc độ fade in/out

    private bool shouldShow = false;
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (loadingArcImage != null)
            loadingArcImage.fillAmount = 0f;
    }

    void Update()
    {
        HandleFade();
        HandleBillboard();
    }

    /// <summary>Fade in khi shouldShow=true, fade out khi false.</summary>
    void HandleFade()
    {
        if (canvasGroup == null) return;
        float target = shouldShow ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
    }

    /// <summary>Luôn xoay Canvas về phía camera (Billboard).</summary>
    void HandleBillboard()
    {
        if (mainCam == null) return;
        transform.LookAt(transform.position + mainCam.transform.rotation * Vector3.forward,
                         mainCam.transform.rotation * Vector3.up);
    }

    /// <summary>Gọi khi player vào vùng tương tác (phiên bản cũ).</summary>
    public void Show(string itemName = "Interact", string action = "Hold E")
    {
        shouldShow = true;
        if (itemNameText != null) itemNameText.text = itemName;
        if (actionText != null) actionText.text = action;
    }

    /// <summary>Gọi khi player vào vùng tương tác (phiên bản mới, có phím).</summary>
    public void Show(string key, string title, string action)
    {
        shouldShow = true;
        if (keyText != null) keyText.text = key;
        if (itemNameText != null) itemNameText.text = title;
        if (actionText != null) actionText.text = action;
    }

    /// <summary>Gọi khi player ra khỏi vùng tương tác.</summary>
    public void Hide()
    {
        shouldShow = false;
    }

    /// <summary>Cập nhật tiến độ vòng tròn loading (0 → 1).</summary>
    public void SetProgress(float progress)
    {
        if (loadingArcImage != null)
            loadingArcImage.fillAmount = progress;
    }
}
