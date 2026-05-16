using UnityEngine;

/// <summary>
/// Gắn vào xe hủ tiếu.
/// Phát hiện player qua SphereCollider Trigger và xử lý Hold-to-Interact.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FoodCartInteract : MonoBehaviour
{
    [Header("Bowl Prefabs")]
    public GameObject huTieuPrefab;
    public GameObject huTieuKhongHanhPrefab;
    public GameObject bunBoPrefab;
    public GameObject bunBoKhongHanhPrefab;

    public string cartName = "Xe Hủ Tiếu";
    public string actionHint = "Giữ E để nấu ăn";

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

        // Nếu chưa có khách gọi món thì hiện thông báo và không cho tương tác
        if (!PlayerInventory.hasActiveOrder)
        {
            if (promptUI != null) promptUI.Show(cartName, "Chưa có khách gọi món!");
            if (isHolding)
            {
                isHolding = false;
                holdTimer = 0f;
                if (promptUI != null) promptUI.SetProgress(0f);
            }
            return;
        }

        // Nếu có khách, đảm bảo hiện đúng hướng dẫn
        if (promptUI != null && !isHolding) promptUI.Show(cartName, actionHint);

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

        // Load scene nấu ăn chồng lên
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MiniGameHuTieu", UnityEngine.SceneManagement.LoadSceneMode.Additive);

        // Ẩn Camera chính của người chơi đi để nhường chỗ cho Camera nấu ăn
        if (Camera.main != null) 
        {
            Camera.main.gameObject.SetActive(false);
        }

        // Khóa player
        if (playerInteract != null)
        {
            PlayerMovement pm = playerInteract.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;
        }

        // Hiện chuột để nấu ăn
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SpawnBowlForPlayer(string recipeName)
    {
        if (playerInteract == null) return;

        GameObject prefabToSpawn = null;
        if (recipeName == "HuTieu") prefabToSpawn = huTieuPrefab;
        else if (recipeName == "HuTieuKhongHanh") prefabToSpawn = huTieuKhongHanhPrefab;
        else if (recipeName == "BunBo") prefabToSpawn = bunBoPrefab;
        else if (recipeName == "BunBoKhongHanh") prefabToSpawn = bunBoKhongHanhPrefab;

        if (prefabToSpawn != null)
        {
            GameObject newBowl = Instantiate(prefabToSpawn);
            playerInteract.ForcePickUp(newBowl);
        }

        // Bật lại Camera chính của người chơi
        Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
        foreach (Camera cam in cameras)
        {
            if (cam.name == "Main Camera")
            {
                cam.gameObject.SetActive(true);
            }
        }

        // Mở khóa player
        PlayerMovement pm = playerInteract.GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = true;

        // Ẩn chuột lại
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
            {
                if (PlayerInventory.hasActiveOrder)
                    promptUI.Show(cartName, actionHint);
                else
                    promptUI.Show(cartName, "Chưa có khách gọi món!");
            }
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
