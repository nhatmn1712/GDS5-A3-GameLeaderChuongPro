using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC ngồi chờ nhận hủ tiếu. Tự động disable patrol/wander và kích hoạt animation ngồi.
/// </summary>
public class NpcCustomer : MonoBehaviour
{
    [Header("Item Settings")]
    public string desiredItem = "HuTieu";
    public bool hasReceivedItem = false;

    [Header("Sitting Setup")]
    [Tooltip("Tên animation clip ngồi trong Animator Controller")]
    public string sittingStateName = "Sitting Idle";
    [Tooltip("Nếu true, NPC sẽ được đặt ở đúng chiều cao ngồi trên ghế")]
    public bool autoAdjustHeight = false;
    public float sittingYOffset = 0f; // Điều chỉnh vị trí Y khi ngồi

    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();

        // ── Tắt di chuyển ──────────────────────────────────
        var patrol = GetComponent<NPCPatrol>();
        if (patrol != null) patrol.enabled = false;

        var wander = GetComponentInParent<NPCRandomWander>();
        if (wander == null) wander = GetComponent<NPCRandomWander>();
        if (wander != null) wander.enabled = false;

        var navAgent = GetComponentInParent<NavMeshAgent>();
        if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
        }

        // ── Kích hoạt animation ngồi ───────────────────────
        if (animator != null)
        {
            // Thử set bool IsSitting nếu có
            foreach (var param in animator.parameters)
            {
                if (param.name == "IsSitting" && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("IsSitting", true);
                    Debug.Log("[NpcCustomer] Đã set IsSitting = true");
                    break;
                }
            }

            // Thử play trực tiếp state "Sitting Idle" nếu tồn tại
            animator.Play(sittingStateName, 0, 0f);
        }

        // ── Điều chỉnh chiều cao nếu cần ──────────────────
        if (autoAdjustHeight)
        {
            var pos = transform.localPosition;
            transform.localPosition = new Vector3(pos.x, sittingYOffset, pos.z);
        }
    }

    /// <summary>Gọi khi nhận được vật phẩm từ TableDelivery.</summary>
    public bool ReceiveItem(string itemName)
    {
        if (!hasReceivedItem && itemName == desiredItem)
        {
            hasReceivedItem = true;
            Debug.Log("[NPC] Cảm ơn nhé! Hủ tiếu ngon quá!");

            // NPC phản ứng: quay đầu về phía tô (nếu muốn)
            // Hoặc trigger animation "Eating" nếu có
            if (animator != null)
            {
                foreach (var param in animator.parameters)
                {
                    if (param.name == "IsEating" && param.type == AnimatorControllerParameterType.Bool)
                    {
                        animator.SetBool("IsEating", true);
                        break;
                    }
                }
            }

            return true;
        }
        return false;
    }
}

