using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite speakerPortrait;
    [TextArea(3, 10)]
    public string dialogueText;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    public CanvasGroup dialogueCanvasGroup;
    public Image portraitImage;
    public Text nameText;
    public Text dialogueText;

    [Header("Settings")]
    public float typingSpeed = 0.05f;

    private Queue<DialogueLine> dialogueLines;
    private bool isTyping = false;
    private string currentSentence = "";
    private Coroutine typingCoroutine;

    // Track if dialogue is currently active to block other player actions if needed
    public bool isDialogueActive { get; private set; }

    private System.Action onDialogueEndCallback;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        dialogueLines = new Queue<DialogueLine>();
        
        // Hide UI on start
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        // Press Space or Left Click to continue
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                // If currently typing, finish the sentence instantly
                CompleteSentence();
            }
            else
            {
                // If done typing, go to next sentence
                DisplayNextSentence();
            }
        }
    }

    public void StartDialogue(DialogueLine[] lines, System.Action onEnd = null)
    {
        onDialogueEndCallback = onEnd;
        isDialogueActive = true;
        dialogueLines.Clear();

        foreach (DialogueLine line in lines)
        {
            dialogueLines.Enqueue(line);
        }

        // Show UI
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 1f;
            dialogueCanvasGroup.interactable = true;
            dialogueCanvasGroup.blocksRaycasts = true;
        }

        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (dialogueLines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueLines.Dequeue();
        
        // Set UI values
        nameText.text = line.speakerName;
        
        if (line.speakerPortrait != null)
        {
            portraitImage.sprite = line.speakerPortrait;
            portraitImage.enabled = true;
            portraitImage.preserveAspect = true;
        }
        else
        {
            portraitImage.enabled = false;
        }

        // Start typing effect
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        currentSentence = line.dialogueText;
        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void CompleteSentence()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        dialogueText.text = currentSentence;
        isTyping = false;
    }

    private void EndDialogue()
    {
        isDialogueActive = false;

        // Hide UI
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
        }

        // Trigger callback if one was provided
        onDialogueEndCallback?.Invoke();
        onDialogueEndCallback = null;
    }
}
