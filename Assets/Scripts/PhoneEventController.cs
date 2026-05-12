using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào GameObject điện thoại (Voxel Universe Objects-46).
/// Khi trời tối, điện thoại sẽ hiện Outline Emission + rung.
/// Prompt UI (World Space) sẽ hiện giống xe hủ tiếu.
/// Người chơi nhấn Y để bắt máy và đoạn Dialogue sẽ bắt đầu.
/// </summary>
public class PhoneEventController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Script DayNightCycle đang chạy trong Scene")]
    public DayNightCycle dayNightCycle;

    [Tooltip("AudioSource gắn trên điện thoại (có clip tiếng chuông)")]
    public AudioSource phoneRingingAudio;

    [Tooltip("InteractPromptUI trên PhoneInteractCanvas (World Space)")]
    public InteractPromptUI phonePromptUI;

    [Header("Dialogue")]
    [Tooltip("Danh sách câu thoại khi nghe máy")]
    public DialogueLine[] parentConversation;

    [Header("Timing")]
    [Tooltip("Mốc thời gian điện thoại bắt đầu reo (giờ, 0-24)")]
    public float timeToRing = 0f;

    [Header("Shake Settings")]
    [Tooltip("Biên độ rung của điện thoại")]
    public float shakeAmount = 0.015f;
    [Tooltip("Tốc độ rung")]
    public float shakeSpeed = 30f;

    [Header("Outline Settings")]
    [Tooltip("Màu Outline khi điện thoại reo")]
    public Color outlineColor = new Color(1f, 0.9f, 0f, 1f); // Yellow
    [Tooltip("Độ sáng Outline")]
    [Range(0.5f, 5f)]
    public float outlineIntensity = 2f;

    [Header("Interaction Settings")]
    [Tooltip("Khoảng cách tối đa để có thể tương tác nghe máy")]
    public float interactDistance = 2.5f;
    private Transform playerTransform;
    private bool playerInRange = false;

    // ---- Private State ----
    private bool hasRungToday = false;
    private bool isRinging = false;
    private Vector3 originalLocalPos;
    private MeshRenderer phoneRenderer;
    private Material phoneMaterial;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Start()
    {
        originalLocalPos = transform.localPosition;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        phoneRenderer = GetComponentInChildren<MeshRenderer>();
        if (phoneRenderer != null)
        {
            phoneMaterial = new Material(phoneRenderer.sharedMaterial);
            phoneRenderer.material = phoneMaterial;
        }

        if (phonePromptUI != null)
            phonePromptUI.Hide();
    }

    private void Update()
    {
        if (dayNightCycle == null) return;

        // Cập nhật trạng thái Player ở gần
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            bool wasInRange = playerInRange;
            playerInRange = dist <= interactDistance;

            // Xử lý hiện/ẩn Prompt UI tuỳ theo khoảng cách (nếu điện thoại đang reo)
            if (isRinging)
            {
                if (playerInRange && !wasInRange && phonePromptUI != null)
                    phonePromptUI.Show("Điện Thoại", "Nhấn Y để nghe máy");
                else if (!playerInRange && wasInRange && phonePromptUI != null)
                    phonePromptUI.Hide();
            }
        }

        if (dayNightCycle.timeOfDay < 5f && hasRungToday)
            hasRungToday = false;

        if (!hasRungToday && dayNightCycle.timeOfDay >= timeToRing)
            StartRinging();

        if (isRinging)
        {
            ShakePhone();

            bool dialogueBusy = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;
            if (!dialogueBusy && playerInRange && Input.GetKeyDown(KeyCode.Y))
                AnswerPhone();
        }
    }

    private void StartRinging()
    {
        hasRungToday = true;
        isRinging = true;

        SetOutline(true);

        if (phoneRingingAudio != null)
            phoneRingingAudio.Play();

        if (phonePromptUI != null)
            phonePromptUI.Show("Điện Thoại", "Nhấn Y để nghe máy");
    }

    private void AnswerPhone()
    {
        isRinging = false;
        transform.localPosition = originalLocalPos;

        SetOutline(false);

        if (phoneRingingAudio != null)
            phoneRingingAudio.Stop();

        if (phonePromptUI != null)
            phonePromptUI.Hide();

        if (DialogueManager.Instance != null && parentConversation != null && parentConversation.Length > 0)
            DialogueManager.Instance.StartDialogue(parentConversation);
        else
            Debug.LogWarning("[PhoneEventController] DialogueManager không tìm thấy hoặc parentConversation chưa được gán!");
    }

    private void ShakePhone()
    {
        float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
        float offsetZ = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeAmount;
        transform.localPosition = originalLocalPos + new Vector3(offsetX, 0f, offsetZ);
    }

    private void SetOutline(bool active)
    {
        if (phoneMaterial == null) return;

        if (active)
        {
            phoneMaterial.EnableKeyword("_EMISSION");
            phoneMaterial.SetColor(EmissionColor, outlineColor * outlineIntensity);
        }
        else
        {
            phoneMaterial.SetColor(EmissionColor, Color.black);
            phoneMaterial.DisableKeyword("_EMISSION");
        }
    }

    private void OnDestroy()
    {
        if (phoneMaterial != null)
            Destroy(phoneMaterial);
    }
}
