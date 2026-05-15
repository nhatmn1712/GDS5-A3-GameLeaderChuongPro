using UnityEngine;

public class CookingCamera : MonoBehaviour
{
    [Header("Immersive Sway Settings")]
    [Tooltip("Khoảng cách camera di chuyển khi bạn lia chuột")]
    public float swayAmount = 0.3f;
    [Tooltip("Độ mượt mà của camera")]
    public float smoothSpeed = 3f;

    private Vector3 startPosition;

    void Start()
    {
        // Lưu lại vị trí ban đầu của camera
        startPosition = transform.position;
    }

    void Update()
    {
        // Lấy vị trí chuột trên màn hình và chuyển nó thành dải từ -1 đến 1
        float mouseX = (Input.mousePosition.x / Screen.width) * 2f - 1f;
        float mouseY = (Input.mousePosition.y / Screen.height) * 2f - 1f;

        // Tính toán vị trí mới dựa trên hướng nhìn của camera (phải/trái, lên/xuống)
        Vector3 targetPosition = startPosition 
                                 + (transform.right * mouseX * swayAmount) 
                                 + (transform.up * mouseY * swayAmount);

        // Di chuyển camera thật mượt mà đến vị trí mới
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
    }
}
