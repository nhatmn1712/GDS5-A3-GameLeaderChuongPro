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

    void Start()
    {
        // Lock and hide the cursor for better camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Get Mouse Input
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Clamp the up/down rotation so camera doesn't flip over
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // 2. Calculate New Position and Rotation
        // Create rotation based on pitch (X axis) and yaw (Y axis)
        Quaternion currentRotation = Quaternion.Euler(pitch, yaw, 0f);
        
        // Calculate the desired position by applying rotation to the offset, then adding to target position
        Vector3 desiredPosition = target.position + currentRotation * offset;

        // Smoothly move the camera
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 3. Make Camera look at the target (adjust slightly up so it looks at the head/body instead of feet)
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}
