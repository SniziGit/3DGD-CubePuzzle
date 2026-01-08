using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TypewriterEffect : MonoBehaviour
{
    [Header("Typing Settings")]
    public float charactersPerSecond = 20f;  // Changed from private to public
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float punctuationDelay = 0.2f;
    [SerializeField] private float cursorBlinkRate = 0.5f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typeSound;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    private TextMeshProUGUI textComponent;
    private string fullText;
    private Coroutine typingCoroutine;
    private Coroutine cursorCoroutine;
    private bool cursorVisible = true;
    private bool isTyping = false;

    public bool IsTyping => isTyping;
    public event System.Action OnTypingComplete;

    private const string CURSOR = "|";
    private const string RICH_CURSOR = "<alpha=#00>|</color>";

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        fullText = textComponent.text;
        textComponent.text = "";
    }

    private void Start()
    {
        StartTyping();
    }

    private void OnEnable()
    {
        if (typingCoroutine == null && !string.IsNullOrEmpty(fullText))
        {
            StartTyping();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        typingCoroutine = null;
        cursorCoroutine = null;
    }

    public void SetText(string text)
    {
        fullText = text;
        if (isActiveAndEnabled)
        {
            StartTyping();
        }
    }

    public void StartTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        if (cursorCoroutine != null)
        {
            StopCoroutine(cursorCoroutine);
        }
        textComponent.text = "";
        isTyping = true;
        typingCoroutine = StartCoroutine(TypeText());
    }

    public void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (cursorCoroutine != null)
        {
            StopCoroutine(cursorCoroutine);
            cursorCoroutine = null;
        }
        textComponent.text = fullText;
        isTyping = false;
        OnTypingComplete?.Invoke();
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        textComponent.text = "";
        float delay = 1f / charactersPerSecond;

        cursorCoroutine = StartCoroutine(BlinkCursor());

        yield return useUnscaledTime ? new WaitForSecondsRealtime(startDelay) : new WaitForSeconds(startDelay);

        for (int i = 0; i < fullText.Length; i++)
        {
            if (cursorCoroutine != null)
            {
                StopCoroutine(cursorCoroutine);
                cursorVisible = true;
            }

            textComponent.text = fullText.Substring(0, i + 1);

            if (audioSource != null && typeSound != null && !char.IsWhiteSpace(fullText[i]))
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.PlayOneShot(typeSound);
            }

            cursorCoroutine = StartCoroutine(BlinkCursor());

            if (i < fullText.Length - 1 && IsPunctuation(fullText[i]))
            {
                yield return useUnscaledTime ? new WaitForSecondsRealtime(punctuationDelay) : new WaitForSeconds(punctuationDelay);
            }
            else
            {
                yield return useUnscaledTime ? new WaitForSecondsRealtime(delay) : new WaitForSeconds(delay);
            }
        }

        if (cursorCoroutine != null)
        {
            StopCoroutine(cursorCoroutine);
        }
        textComponent.text = fullText;
        isTyping = false;
        OnTypingComplete?.Invoke();
    }

    private IEnumerator BlinkCursor()
    {
        while (true)
        {
            if (cursorVisible)
            {
                textComponent.text = textComponent.text + CURSOR;
            }
            else
            {
                if (textComponent.text.Length > 0 && textComponent.text[textComponent.text.Length - 1].ToString() == CURSOR)
                {
                    textComponent.text = textComponent.text.Substring(0, textComponent.text.Length - 1);
                }
            }
            cursorVisible = !cursorVisible;
            yield return useUnscaledTime ? new WaitForSecondsRealtime(cursorBlinkRate) : new WaitForSeconds(cursorBlinkRate);
        }
    }

    private bool IsPunctuation(char character)
    {
        return character == '.' || character == '!' || character == '?' ||
               character == ',' || character == ';' || character == ':' ||
               character == '-';
    }
}