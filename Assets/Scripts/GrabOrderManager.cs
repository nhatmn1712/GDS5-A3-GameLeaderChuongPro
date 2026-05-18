using UnityEngine;

/// <summary>
/// Central singleton for managing Grab delivery orders.
/// Randomly generates orders, calculates distance-based pricing,
/// and tracks the active delivery state.
/// </summary>
public class GrabOrderManager : MonoBehaviour
{
    public static GrabOrderManager Instance { get; private set; }

    [Header("Delivery Spots")]
    [Tooltip("Drag all GrabDeliverySpot objects from the scene here.")]
    public GrabDeliverySpot[] deliverySpots;

    [Header("Food Cart Reference")]
    [Tooltip("Drag the Hu Tieu cart (the order spot) here to measure delivery distance.")]
    public Transform cartTransform;

    [Header("Order Timing")]
    [Tooltip("How many seconds between each chance for a new order (default: 90s).")]
    public float checkIntervalSeconds = 90f;
    [Tooltip("Probability (0-1) of an order being generated each interval. 0.2 = 20%.")]
    [Range(0f, 1f)]
    public float orderChance = 0.2f;
    [Tooltip("Seconds to wait after a rejection before a new order can appear.")]
    public float rejectionCooldown = 30f;

    [Header("Pricing")]
    [Tooltip("Minimum pay for any delivery order.")]
    public int minPay = 2;
    [Tooltip("Pay increases by $1 for every X units of distance.")]
    public float distancePerExtraDollar = 30f;

    // ─── Public State ───────────────────────────────────────────────
    public bool HasActiveOrder { get; private set; } = false;
    public bool HasPendingNotification { get; private set; } = false;

    public string OrderedFood   { get; private set; } = "";
    public string LocationName  { get; private set; } = "";
    public int    EstimatedPay  { get; private set; } = 0;
    public GrabDeliverySpot ActiveSpot { get; private set; } = null;

    // ─── Private ────────────────────────────────────────────────────
    private float timer = 0f;
    private float cooldownTimer = 0f;
    private bool inCooldown = false;

    private static readonly string[] foods = 
        { "HuTieu", "HuTieuKhongHanh", "BunBo", "BunBoKhongHanh" };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Start the first check after a delay so the player can settle in
        timer = checkIntervalSeconds * 0.5f;
    }

    void Update()
    {
        if (inCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f) inCooldown = false;
            return;
        }

        // Don't generate a new order if one is already active or pending
        if (HasActiveOrder || HasPendingNotification) return;

        // Don't generate Grab orders if the physical shop is closed!
        if (RestaurantManager.Instance != null && !RestaurantManager.Instance.isShopOpen) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = checkIntervalSeconds;

            if (Random.value <= orderChance)
                GenerateOrder();
        }
    }

    void GenerateOrder()
    {
        // Only generate if there are available (inactive) spots
        GrabDeliverySpot spot = GetRandomAvailableSpot();
        if (spot == null) return;

        // Pick a random food
        OrderedFood = foods[Random.Range(0, foods.Length)];
        ActiveSpot  = spot;
        LocationName = spot.locationName;

        // Calculate distance-based pay
        if (cartTransform != null)
        {
            float dist = Vector3.Distance(cartTransform.position, spot.transform.position);
            int extra = Mathf.FloorToInt(dist / distancePerExtraDollar);
            EstimatedPay = minPay + extra;
        }
        else
        {
            EstimatedPay = minPay;
        }

        HasPendingNotification = true;
        Debug.Log($"[Grab] New order generated! Food: {OrderedFood}, Spot: {LocationName}, Pay: ${EstimatedPay}");

        // Tell the phone UI to shake the icon
        if (PhoneUIManager.Instance != null)
            PhoneUIManager.Instance.OnGrabNotificationReceived();
    }

    /// <summary>Player clicks Accept on the order panel.</summary>
    public void AcceptOrder()
    {
        if (!HasPendingNotification) return;

        HasPendingNotification = false;
        HasActiveOrder = true;

        // Block walk-in orders while delivery is active
        PlayerInventory.hasActiveOrder = true;
        PlayerInventory.activeDeliveryFood = OrderedFood;

        // Activate the delivery spot so it can receive the player
        if (ActiveSpot != null)
            ActiveSpot.Activate(OrderedFood);

        // Show the delivery arrow
        if (DeliveryArrowUI.Instance != null)
            DeliveryArrowUI.Instance.SetTarget(ActiveSpot.transform);

        Debug.Log($"[Grab] Order accepted! Deliver {OrderedFood} to {LocationName}.");
    }

    /// <summary>Player clicks Reject on the order panel.</summary>
    public void RejectOrder()
    {
        HasPendingNotification = false;
        HasActiveOrder = false;
        ActiveSpot = null;
        OrderedFood = "";
        EstimatedPay = 0;

        inCooldown = true;
        cooldownTimer = rejectionCooldown;

        Debug.Log("[Grab] Order rejected. Cooldown started.");
    }

    /// <summary>Called by GrabDeliverySpot when the player successfully delivers.</summary>
    public void CompleteDelivery()
    {
        MoneyManager.AddMoney(EstimatedPay);
        Debug.Log($"[Grab] Delivery complete! Earned ${EstimatedPay}.");

        HasActiveOrder = false;
        PlayerInventory.hasActiveOrder = false;
        PlayerInventory.activeDeliveryFood = "";

        if (DeliveryArrowUI.Instance != null)
            DeliveryArrowUI.Instance.HideArrow();

        if (ActiveSpot != null)
            ActiveSpot.Deactivate();

        ActiveSpot = null;
        OrderedFood = "";
        EstimatedPay = 0;

        // Reset timer so next order won't fire immediately
        timer = checkIntervalSeconds;
    }

    /// <summary>Called by GrabDeliverySpot when the player delivers the WRONG food.</summary>
    public void WrongFoodDelivered()
    {
        // Player keeps the hasActiveOrder = true so they can go back and cook again
        Debug.Log("[Grab] Wrong food delivered! Player needs to try again.");
    }

    GrabDeliverySpot GetRandomAvailableSpot()
    {
        if (deliverySpots == null || deliverySpots.Length == 0) return null;

        // Shuffle-pick to avoid repeats
        GrabDeliverySpot[] shuffled = (GrabDeliverySpot[])deliverySpots.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        foreach (var spot in shuffled)
            if (spot != null && !spot.IsActive) return spot;

        return null;
    }

    /// <summary>English food name for UI display.</summary>
    public static string GetFoodDisplayName(string code)
    {
        return code switch
        {
            "HuTieu"           => "Hu Tieu (with scallions)",
            "HuTieuKhongHanh"  => "Hu Tieu (no scallions)",
            "BunBo"            => "Bun Bo (with scallions)",
            "BunBoKhongHanh"   => "Bun Bo (no scallions)",
            _                  => code
        };
    }
}
