using UnityEngine;

/// <summary>
/// Gắn vào cùng GameObject với Animator (child của NPC).
/// Dùng OnAnimatorIK để lock head bone sau khi Animator xử lý xong.
/// </summary>
[RequireComponent(typeof(Animator))]
public class NpcHeadLock : MonoBehaviour
{
    private Animator anim;
    private NpcCustomer npcCustomer;
    private bool isLocked = false;

    void Awake()
    {
        anim = GetComponent<Animator>();
        // Tìm NpcCustomer trên parent
        npcCustomer = GetComponentInParent<NpcCustomer>();
    }

    void Update()
    {
        if (npcCustomer == null) return;
        isLocked = (npcCustomer.currentState == NpcCustomer.NpcState.Eating);
    }

    // OnAnimatorIK chạy SAU khi Animator tính toán xong tất cả bone rotation
    // Đây là cách chính xác để override Humanoid bone rotation
    void OnAnimatorIK(int layerIndex)
    {
        if (!isLocked) return;

        // Set Look At Weight = 0 để tắt bất kỳ Look At IK nào
        anim.SetLookAtWeight(0f);

        // Lock head bone về Quaternion.identity (nhìn thẳng theo neck)
        anim.SetBoneLocalRotation(HumanBodyBones.Head, Quaternion.identity);
        anim.SetBoneLocalRotation(HumanBodyBones.Neck, Quaternion.identity);
    }
}
