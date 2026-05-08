using UnityEngine;

public class FoodCart : MonoBehaviour
{
    public GameObject noodleBowlPrefab; // Kéo prefab tô hủ tiếu vào đây
    public string promptText = "Nhấn E để lấy hủ tiếu";

    // Trả về một tô hủ tiếu mới
    public GameObject GetNoodleBowl()
    {
        if (noodleBowlPrefab != null)
        {
            return Instantiate(noodleBowlPrefab);
        }
        else
        {
            Debug.LogWarning("Chưa gán Prefab tô hủ tiếu cho Xe Hủ Tiếu!");
            return null;
        }
    }
}
