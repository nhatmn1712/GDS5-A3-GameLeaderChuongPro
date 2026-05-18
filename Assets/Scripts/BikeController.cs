using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BikeController : MonoBehaviour
{
    [Header("Speed & Turning")]
    public float driveSpeed = 12f;
    public float reverseSpeed = 5f;
    public float turnSpeed = 100f;
    public float gravity = -9.81f;

    [Header("References")]
    [Tooltip("The Player GameObject that will disappear when riding.")]
    public GameObject playerObject;
    
    [Tooltip("Your normal Third-Person Camera in the scene.")]
    public GameObject mainCamera;
    
    [Tooltip("The First-Person Camera attached to this bike.")]
    public GameObject bikeCamera;
    
    [Tooltip("The UI Canvas that shows 'Press E to Interact' (we'll use it for F).")]
    public InteractPromptUI interactUI;

    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.F;
    public float interactDistance = 3.5f;

    private CharacterController controller;
    private bool isRiding = false;
    private bool playerInRange = false;
    private float velocityY = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Ensure the bike camera starts turned off
        if (bikeCamera != null)
        {
            bikeCamera.SetActive(false);
        }
    }

    void Update()
    {
        if (isRiding)
        {
            // --- WE ARE RIDING THE BIKE ---
            DriveBike();

            // Check if player wants to get off
            if (Input.GetKeyDown(interactKey))
            {
                GetOffBike();
            }
        }
        else
        {
            // --- WE ARE WALKING ---
            CheckPlayerDistance();
        }
    }

    void CheckPlayerDistance()
    {
        if (playerObject == null || !playerObject.activeInHierarchy) return;

        float distance = Vector3.Distance(transform.position, playerObject.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactDistance;

        // Show/Hide UI prompt
        if (playerInRange && !wasInRange && interactUI != null)
        {
            interactUI.Show("Bicycle", "Press F to ride");
        }
        else if (!playerInRange && wasInRange && interactUI != null)
        {
            interactUI.Hide();
        }

        // Get ON the bike
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            GetOnBike();
        }
    }

    void GetOnBike()
    {
        isRiding = true;

        if (interactUI != null) interactUI.Hide();

        // 1. Hide the Player
        if (playerObject != null)
            playerObject.SetActive(false);

        // 2. Switch Cameras
        if (mainCamera != null)
            mainCamera.SetActive(false);

        if (bikeCamera != null)
            bikeCamera.SetActive(true);

        Debug.Log("[Bike] Got ON the bike.");
    }

    void GetOffBike()
    {
        isRiding = false;

        // 1. Spawn player slightly to the left of the bike so they don't get stuck inside it
        if (playerObject != null)
        {
            playerObject.transform.position = transform.position - transform.right * 1.5f;
            playerObject.SetActive(true);
        }

        // 2. Switch Cameras Back
        if (bikeCamera != null)
            bikeCamera.SetActive(false);

        if (mainCamera != null)
            mainCamera.SetActive(true);

        Debug.Log("[Bike] Got OFF the bike.");
    }

    void DriveBike()
    {
        // Simple WASD input
        float moveInput = Input.GetAxis("Vertical");   // W / S
        float turnInput = Input.GetAxis("Horizontal"); // A / D

        // Calculate speed (forward is faster than reverse)
        float currentSpeed = moveInput > 0 ? driveSpeed : reverseSpeed;

        // Turn the bike Left/Right
        transform.Rotate(Vector3.up * turnInput * turnSpeed * Time.deltaTime);

        // Move the bike Forward/Backward
        Vector3 moveDirection = transform.forward * moveInput * currentSpeed;

        // Apply Gravity so the bike doesn't float into the sky
        if (controller.isGrounded)
        {
            velocityY = -2f; 
        }
        else
        {
            velocityY += gravity * Time.deltaTime;
        }
        
        moveDirection.y = velocityY;

        // Actually move the bike
        controller.Move(moveDirection * Time.deltaTime);
    }
}
