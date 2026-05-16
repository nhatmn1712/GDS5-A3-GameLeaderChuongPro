using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;

    private CharacterController controller;
    private Vector3 velocity;
    
    // Reference to Animator for playing animations
    public Animator animator;

    // Camera reference
    public Transform cameraTransform;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // If cameraTransform is not assigned, try to use the main camera
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void OnEnable()
    {
        // Reset velocity khi được bật lại (ví dụ: sau mini-game nấu ăn)
        // Tránh trường hợp velocity.y âm tích lũy từ trước gây "rớt xuống"
        velocity = Vector3.zero;
    }

    void Update()
    {
        MovePlayer();
        ApplyGravity();
    }

    void MovePlayer()
    {
        // 1. Get WASD or Arrow Key Input
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down

        // 2. Calculate movement direction based on camera angle
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            // Calculate target angle based on camera rotation
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            
            // Smoothly rotate the character towards the target angle
            float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, Time.deltaTime * rotationSpeed);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Calculate the actual move direction based on the target angle
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            
            // Move the character
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);

            // Play walking/running animation
            if (animator != null) animator.SetBool("IsMoving", true);
        }
        else
        {
            // Play idle animation
            if (animator != null) animator.SetBool("IsMoving", false);
        }
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        velocity.y += gravity * Time.deltaTime;

        // Clamp để velocity.y không tích lũy xuống âm vô hạn (bug phổ biến của CharacterController)
        velocity.y = Mathf.Max(velocity.y, gravity);

        controller.Move(velocity * Time.deltaTime);
    }
}
