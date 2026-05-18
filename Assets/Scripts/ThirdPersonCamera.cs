using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;          // The player character to follow
    public Vector3 offset = new Vector3(0f, 2f, -5f); // Position offset relative to the target
    
    public float smoothSpeed = 5f;    // How smoothly the camera catches up
    public float mouseSensitivity = 3f;

    private float pitch = 0f;         // Up/Down rotation
    private float yaw = 0f;           // Left/Right rotation

    public float pitchMin = -20f;     // Lowest camera angle
    public float pitchMax = 60f;      // Highest camera angle

    [Header("First Person Settings")]
    public bool isFirstPerson = false;
    public Vector3 firstPersonOffset = new Vector3(0f, 1.8f, 0.3f); // Vị trí đầu nhân vật (hơi nhô ra trước để không bị kẹt vào mesh)
    private Vector3 thirdPersonOffset;

    void Start()
    {
        // Lock and hide the cursor for better camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        thirdPersonOffset = offset;
    }

    void Update()
    {
        // Nhấn phím B để chuyển đổi góc nhìn
        if (Input.GetKeyDown(KeyCode.B))
        {
            isFirstPerson = !isFirstPerson;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Get Mouse Input
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Clamp the up/down rotation so camera doesn't flip over
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Create rotation based on pitch (X axis) and yaw (Y axis)
        Quaternion currentRotation = Quaternion.Euler(pitch, yaw, 0f);
        
        if (isFirstPerson)
        {
            // Góc nhìn thứ nhất (First Person)
            // Xoay offset Z theo hướng ngang của camera để đẩy camera ra phía trước mặt nhân vật
            Quaternion horizontalRot = Quaternion.Euler(0, yaw, 0);
            Vector3 headPos = target.position + Vector3.up * firstPersonOffset.y + horizontalRot * new Vector3(0, 0, firstPersonOffset.z);
            
            transform.position = headPos;
            transform.rotation = currentRotation;
        }
        else
        {
            // Góc nhìn thứ ba (Third Person)
            // Calculate the desired position by applying rotation to the offset, then adding to target position
            Vector3 desiredPosition = target.position + currentRotation * thirdPersonOffset;

            // Smoothly move the camera
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Make Camera look at the target
            transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }
}
