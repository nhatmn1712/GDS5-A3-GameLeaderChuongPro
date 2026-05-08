using UnityEngine;

/// <summary>
/// Gắn vào xe hủ tiếu.
/// Phát hiện player qua SphereCollider Trigger và xử lý Hold-to-Interact.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FoodCartInteract : MonoBehaviour
{
    [Header("Item Settings")]
    public GameObject noodleBowlPrefab;      // Kéo Prefab Tô Hủ Tiếu vào đây
    public string cartName = "Xe Hủ Tiếu";
    public string actionHint = "Giữ E để lấy hủ tiếu";

    [Header("Interaction Settings")]
    public float holdDuration = 1.5f;        // Thời gian giữ E để lấy (giây)
    public float detectRange = 2.5f;         // Bán kính phát hiện player
    public KeyCode interactKey = KeyCode.E;

    [Header("UI")]
    public InteractPromptUI promptUI;        // Kéo InteractCanvas/Panel vào đây

    [Header("Hold Point")]
    public Transform playerHoldPoint;        // Điểm cầm tô hủ tiếu trên player - gán qua code

    // Trạng thái
    private bool playerInRange = false;
    private bool isHolding = false;
    private float holdTimer = 0f;
    private Transform playerTransform = null;
    private PlayerInteract playerInteract = null;

    void Start()
    {
        // Cấu hình SphereCollider Trigger
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = detectRange;

        if (promptUI != null)
            promptUI.Hide();
    }

    void Update()
    {
        if (!playerInRange) return;

        bool ePressed = Input.GetKey(interactKey);

        if (ePressed)
        {
            // Kiểm tra player có đang cầm đồ rồi không
            if (playerInteract != null && playerInteract.IsHoldingItem())
            {
                // Không lấy thêm khi đang cầm rồi
                if (promptUI != null)
                    promptUI.SetProgress(0f);
                return;
            }

            isHolding = true;
            holdTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(holdTimer / holdDuration);
            if (promptUI != null) promptUI.SetProgress(progress);

            if (holdTimer >= holdDuration)
            {
                OnInteractComplete();
            }
        }
        else
        {
            // Thả tay → reset
            if (isHolding)
            {
                isHolding = false;
                holdTimer = 0f;
                if (promptUI != null) promptUI.SetProgress(0f);
            }
        }
    }

    void OnInteractComplete()
    {
        holdTimer = 0f;
        isHolding = false;
        if (promptUI != null) promptUI.SetProgress(0f);

        if (playerInteract != null && noodleBowlPrefab != null)
        {
            // Spawn tô hủ tiếu và giao cho player
            GameObject newBowl = Instantiate(noodleBowlPrefab);
            playerInteract.ForcePickUp(newBowl);
            Debug.Log("[FoodCart] Đã lấy tô hủ tiếu thành công!");
        }
        else
        {
            Debug.LogWarning("[FoodCart] Chưa gán Prefab tô hủ tiếu hoặc PlayerInteract!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = true;
            playerTransform = other.transform;
            playerInteract = pi;

            if (promptUI != null)
                promptUI.Show(cartName, actionHint);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = false;
            playerTransform = null;
            playerInteract = null;
            holdTimer = 0f;
            isHolding = false;

            if (promptUI != null)
            {
                promptUI.SetProgress(0f);
                promptUI.Hide();
            }
        }
    }

    // Vẽ phạm vi detect trong Scene View để dễ chỉnh
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, detectRange);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
