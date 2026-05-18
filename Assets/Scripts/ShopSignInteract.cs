using UnityEngine;

public class ShopSignInteract : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Drag an InteractCanvas here to show the E prompt.")]
    public InteractPromptUI promptUI;

    [Header("Detection")]
    [Tooltip("If the sign's pivot is offset, create an empty child on the mesh and drag it here to act as the true center.")]
    public Transform interactCenter;
    public float interactRadius = 2.5f;

    [Header("Visuals (Optional)")]
    [Tooltip("If true, the sign will spin 180 degrees when toggled.")]
    public bool rotateOnToggle = false;

    private PlayerInteract currentPlayer;
    private bool playerInRange = false;

    void Start()
    {
        if (promptUI != null) promptUI.Hide();
        currentPlayer = FindObjectOfType<PlayerInteract>(true);
    }

    void Update()
    {
        if (currentPlayer == null)
            currentPlayer = FindObjectOfType<PlayerInteract>(true);

        if (currentPlayer == null || RestaurantManager.Instance == null) return;

        // Determine which point to use for distance checking
        Vector3 centerPoint = interactCenter != null ? interactCenter.position : transform.position;

        // Check Distance
        playerInRange = false;
        if (currentPlayer.gameObject.activeInHierarchy)
        {
            if (Vector3.Distance(centerPoint, currentPlayer.transform.position) <= interactRadius)
                playerInRange = true;
        }
        else
        {
            // Check if player is on the bike
            BikeController bike = FindObjectOfType<BikeController>();
            if (bike != null && bike.gameObject.activeInHierarchy)
            {
                if (Vector3.Distance(centerPoint, bike.transform.position) <= interactRadius)
                    playerInRange = true;
            }
        }

        // Handle Interaction
        if (playerInRange)
        {
            bool isOpen = RestaurantManager.Instance.isShopOpen;

            if (promptUI != null)
            {
                if (isOpen)
                    promptUI.Show("Shop Sign", "Press E to CLOSE shop");
                else
                    promptUI.Show("Shop Sign", "Press E to OPEN shop");
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleShop();
            }
        }
        else
        {
            if (promptUI != null) promptUI.Hide();
        }
    }

    void ToggleShop()
    {
        bool isOpen = !RestaurantManager.Instance.isShopOpen;
        RestaurantManager.Instance.isShopOpen = isOpen;

        if (isOpen)
            Debug.Log("[ShopSign] The shop is now OPEN!");
        else
            Debug.Log("[ShopSign] The shop is now CLOSED!");

        if (rotateOnToggle)
        {
            Vector3 centerPoint = interactCenter != null ? interactCenter.position : transform.position;
            transform.RotateAround(centerPoint, Vector3.up, 180f);
        }

        // Update the prompt immediately after pressing E
        if (promptUI != null)
        {
            if (isOpen)
                promptUI.Show("Shop Sign", "Press E to CLOSE shop");
            else
                promptUI.Show("Shop Sign", "Press E to OPEN shop");
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 centerPoint = interactCenter != null ? interactCenter.position : transform.position;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        Gizmos.DrawSphere(centerPoint, interactRadius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(centerPoint, interactRadius);
    }
}
