using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class NpcOrderInteract : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The NpcCustomer component on this NPC (will auto-find if empty).")]
    public NpcCustomer customer;
    [Tooltip("The UI Panel that shows the order. Drag the Canvas/Panel here.")]
    public InteractPromptUI promptUI;
    
    [Header("Settings")]
    public string orderText = "I want 1 To Hu Tieu";
    public string actionHint = "press E to confirm order";
    [Tooltip("How close the player needs to be to see the UI and interact.")]
    public float detectRange = 2.5f;

    private bool playerInRange = false;
    private bool isReadyToOrder = false;
    private bool orderConfirmed = false;

    void Start()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = detectRange;

        if (promptUI != null) promptUI.Hide();
        
        if (customer == null) 
            customer = GetComponentInParent<NpcCustomer>();
    }

    void Update()
    {
        if (customer != null && customer.currentState == NpcCustomer.NpcState.Ordering)
        {
            if (!isReadyToOrder && !orderConfirmed)
            {
                isReadyToOrder = true;
                
                if (playerInRange && promptUI != null)
                {
                    promptUI.Show(orderText, actionHint);
                }
            }
        }
        else
        {
            orderConfirmed = false;
            isReadyToOrder = false;
        }

        if (isReadyToOrder && playerInRange)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                ConfirmOrder();
            }
        }
    }

    void ConfirmOrder()
    {
        isReadyToOrder = false;
        orderConfirmed = true;

        if (promptUI != null) promptUI.Hide();
        
        Debug.Log("[NpcOrder] Order confirmed by player!");
        
        if (customer != null) 
        {
            customer.OnOrderConfirmed();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerInteract pi = other.GetComponent<PlayerInteract>();
        if (pi == null) pi = other.GetComponentInParent<PlayerInteract>();

        if (pi != null)
        {
            playerInRange = true;
            if (isReadyToOrder && promptUI != null)
            {
                promptUI.Show(orderText, actionHint);
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
            if (promptUI != null) promptUI.Hide();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(transform.position, detectRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
