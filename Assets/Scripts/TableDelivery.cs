using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TableDelivery : MonoBehaviour
{
    public enum TableState { WaitingForFood, WrongFood, Eating, WaitingForPayment }
    public TableState currentState = TableState.WaitingForFood;

    [Header("Settings")]
    public KeyCode deliverKey = KeyCode.E;
    [Tooltip("Điều chỉnh kích thước tô khi đặt lên bàn (chỉnh giống thông số Hold Scale của Player)")]
    public Vector3 tableScale = new Vector3(1f, 1f, 1f);

    [Header("Full Bowl Prefabs (When Delivered)")]
    public GameObject huTieuFull;
    public GameObject huTieuKhongHanhFull;
    public GameObject bunBoFull;
    public GameObject bunBoKhongHanhFull;

    [Header("Empty Bowl Prefabs (After Eating)")]
    public GameObject emptyWhiteBowl; // Cho Hủ Tiếu
    public GameObject emptyYellowBowl; // Cho Bún Bò

    [Header("Placement")]
    public Transform bowlPlacePoint;        
    
    [Header("UI (optional)")]
    public InteractPromptUI promptUI;

    [Header("NPC Reaction")]
    public Transform chairWaypoint;
    public NpcCustomer linkedNpc;           

    private GameObject spawnedBowl = null;
    private bool playerInRange = false;
    private PlayerInteract currentPlayer = null;

    void Start()
    {
        this.enabled = false;
    }

    void OnEnable()
    {
        currentState = TableState.WaitingForFood;
        ClearTable();
        playerInRange = false;
        currentPlayer = null;

        InteractPromptUI activeUI = GetActivePromptUI();
        if (activeUI != null) activeUI.Hide();
            
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnDisable()
    {
        ClearTable();
        InteractPromptUI activeUI = GetActivePromptUI();
        if (activeUI != null) activeUI.Hide();
        playerInRange = false;
        currentPlayer = null;
    }

    // Lấy UI trên đầu NPC (nếu có) để hiện thông báo cho tự nhiên
    private InteractPromptUI GetActivePromptUI()
    {
        if (linkedNpc != null)
        {
            NpcOrderInteract npcOrder = linkedNpc.GetComponentInChildren<NpcOrderInteract>();
            if (npcOrder != null && npcOrder.promptUI != null)
            {
                return npcOrder.promptUI;
            }
        }
        return promptUI;
    }

    void Update()
    {
        if (!playerInRange || currentPlayer == null || linkedNpc == null) return;

        InteractPromptUI activeUI = GetActivePromptUI();

        if (currentState == TableState.WaitingForFood)
        {
            if (currentPlayer.IsHoldingItem())
            {
                if (activeUI != null) activeUI.Show("Bàn", "Nhấn E để đặt đồ ăn");
                if (Input.GetKeyDown(deliverKey))
                {
                    PlaceFood();
                }
            }
            else
            {
                if (activeUI != null) activeUI.Hide();
            }
        }
        else if (currentState == TableState.WrongFood)
        {
            if (activeUI != null) activeUI.Show("Sai món!", "Khách không gọi món này! Nhấn F để dọn.");
            if (Input.GetKeyDown(KeyCode.F))
            {
                ClearTable();
                currentState = TableState.WaitingForFood;
                if (activeUI != null) activeUI.Hide();
            }
        }
        else if (currentState == TableState.WaitingForPayment)
        {
            if (activeUI != null) activeUI.Show("Tính tiền", "Nhấn F để thu tiền");
            if (Input.GetKeyDown(KeyCode.F))
            {
                CollectPayment();
            }
        }
        else
        {
            if (activeUI != null) activeUI.Hide(); // Đang ăn
        }
    }

    void PlaceFood()
    {
        string deliveredItem = PlayerInventory.carryingBowl;
        currentPlayer.DropItemForDelivery();
        PlayerInventory.carryingBowl = "";

        SpawnBowl(deliveredItem, false);

        if (deliveredItem == linkedNpc.desiredItem)
        {
            currentState = TableState.Eating;
            PlayerInventory.hasActiveOrder = false; // Cho phép xe nhận order mới
            linkedNpc.ReceiveItem(deliveredItem);
            
            InteractPromptUI activeUI = GetActivePromptUI();
            if (activeUI != null) activeUI.Hide();
        }
        else
        {
            currentState = TableState.WrongFood;
        }
    }

    public void OnNPCFinishedEating()
    {
        currentState = TableState.WaitingForPayment;
        SpawnBowl(linkedNpc.desiredItem, true); // Đổi thành tô rỗng
    }

    void CollectPayment()
    {
        MoneyManager.AddMoney(1);
        ClearTable();
        
        if (linkedNpc != null)
        {
            linkedNpc.StandUpAndLeave(); // Kêu NPC đứng dậy đi về
        }
        
        currentState = TableState.WaitingForFood;
        InteractPromptUI activeUI = GetActivePromptUI();
        if (activeUI != null) activeUI.Hide();
        Debug.Log("[Table] Đã thu tiền!");
    }

    void SpawnBowl(string recipeName, bool isEmpty)
    {
        ClearTable();
        GameObject prefabToSpawn = null;

        if (isEmpty)
        {
            if (recipeName.Contains("HuTieu")) prefabToSpawn = emptyWhiteBowl;
            else if (recipeName.Contains("BunBo")) prefabToSpawn = emptyYellowBowl;
        }
        else
        {
            if (recipeName == "HuTieu") prefabToSpawn = huTieuFull;
            else if (recipeName == "HuTieuKhongHanh") prefabToSpawn = huTieuKhongHanhFull;
            else if (recipeName == "BunBo") prefabToSpawn = bunBoFull;
            else if (recipeName == "BunBoKhongHanh") prefabToSpawn = bunBoKhongHanhFull;
        }

        if (prefabToSpawn != null)
        {
            Vector3 placePos = bowlPlacePoint != null ? bowlPlacePoint.position : transform.position + Vector3.up * 0.1f;
            spawnedBowl = Instantiate(prefabToSpawn, placePos, Quaternion.identity);
            
            // Set parents to the table so it moves if the table moves, and apply scale
            if (bowlPlacePoint != null) spawnedBowl.transform.SetParent(bowlPlacePoint);
            else spawnedBowl.transform.SetParent(transform);
            
            spawnedBowl.transform.localScale = tableScale; // Áp dụng kích thước thu nhỏ
        }
    }

    void ClearTable()
    {
        if (spawnedBowl != null)
        {
            Destroy(spawnedBowl);
            spawnedBowl = null;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!this.enabled) return;

        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = true;
            currentPlayer = pi;
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = false;
            currentPlayer = null;
            InteractPromptUI activeUI = GetActivePromptUI();
            if (activeUI != null) activeUI.Hide();
        }
    }
}
