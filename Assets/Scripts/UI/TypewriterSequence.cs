using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum UIAnimationType
{
    Scroll
}

public enum SequenceActionType
{
    TypeText,     // Type out text with TypewriterEffect
    Wait,         // Wait for specified seconds
    WaitForInput, // Wait for user input (e.g., mouse click)
    UIAnimation,  // Play a UI animation
    SetActive,    // Set active state of a GameObject
}

[System.Serializable]
public class SequenceStep
{
    public SequenceActionType actionType;

    [Header("TypeText Settings")]
    public TextMeshProUGUI targetText;
    [TextArea(3, 10)]
    public string textToType = "";
    public float typeSpeed = 20f;
    public bool waitForCompletion = true;

    [Header("Wait Settings")]
    public float duration = 1f;

    [Header("UI Animation Settings")]
    public UIAnimationType uiAnimationType;
    public RectTransform targetPanel;
    public Vector2 startPosition;
    public Vector2 endPosition;
    public float animationDuration = 1f;
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Set Active Settings")]
    public GameObject targetObject;
    public bool setActiveState = true;

    [Header("Skip Settings")]
    public bool enableSkip = false;
    [Tooltip("Index of the step to skip to (0-based)")]
    public int skipToIndex = -1;
}

public class TypewriterSequence : MonoBehaviour
{
    [Header("Sequence Settings")]
    public bool playOnStart = true;
    public bool playOnAwake = false;
    public bool loopSequence = false;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private SequenceStep[] sequenceSteps;

    [Header("Typewriter Audio Settings")]
    [SerializeField] private AudioSource typewriterAudioSource;
    [SerializeField] private AudioClip typewriterSound;
    [SerializeField] private float typewriterMinPitch = 0.9f;
    [SerializeField] private float typewriterMaxPitch = 1.1f;

    private int currentStepIndex = 0;
    private TypewriterEffect currentTypewriter;
    private bool isSequenceRunning = false;
    private bool isWaitingForInput = false;

    private void Start()
    {
        if (playOnStart)
        {
            StartSequence();
        }
    }
    private void Awake()
    {
        // Ensure we have a typewriter audio source
        if (typewriterAudioSource == null)
        {
            typewriterAudioSource = gameObject.AddComponent<AudioSource>();
            typewriterAudioSource.playOnAwake = false;
        }

        if(playOnAwake)
        {
            StartSequence();
        }
    }

    private void Update()
    {
        if (isWaitingForInput && Input.GetMouseButtonDown(0))
        {
            isWaitingForInput = false;
        }

        // Handle skip functionality
        if (isSequenceRunning && Input.GetMouseButtonDown(0))
        {
            CheckAndPerformSkip();
        }
    }

    public void StartSequence()
    {
        if (isSequenceRunning) return;

        currentStepIndex = 0;
        isSequenceRunning = true;
        ProcessCurrentStep();
    }

    public void StopSequence()
    {
        if (currentTypewriter != null)
        {
            currentTypewriter.StopAllCoroutines();
            currentTypewriter = null;
        }
        StopAllCoroutines();
        isSequenceRunning = false;
        isWaitingForInput = false;
    }

    private void ProcessCurrentStep()
    {
        if (currentStepIndex >= sequenceSteps.Length)
        {
            if (loopSequence)
            {
                currentStepIndex = 0;
            }
            else
            {
                isSequenceRunning = false;
                return;
            }
        }

        SequenceStep currentStep = sequenceSteps[currentStepIndex];

        switch (currentStep.actionType)
        {
            case SequenceActionType.TypeText:
                StartCoroutine(HandleTypeTextStep(currentStep));
                break;

            case SequenceActionType.Wait:
                StartCoroutine(HandleWaitStep(currentStep.duration));
                break;

            case SequenceActionType.WaitForInput:
                StartCoroutine(HandleWaitForInputStep());
                break;

            case SequenceActionType.UIAnimation:
                StartCoroutine(HandleUIAnimationStep(currentStep));
                break;

            case SequenceActionType.SetActive:
                HandleSetActiveStep(currentStep);
                break;
        }
    }

    private IEnumerator HandleTypeTextStep(SequenceStep step)
    {
        if (step.targetText == null)
        {
            Debug.LogWarning("No target text assigned for typewriter step!");
            MoveToNextStep();
            yield break;
        }

        currentTypewriter = step.targetText.GetComponent<TypewriterEffect>();
        if (currentTypewriter == null)
        {
            currentTypewriter = step.targetText.gameObject.AddComponent<TypewriterEffect>();
        }

        // Configure the typewriter effect with our dedicated audio source
        currentTypewriter.charactersPerSecond = step.typeSpeed;
        
        // Use reflection or public method to set audio source if available
        // For now, we'll access the private field through the component
        var audioSourceField = typeof(TypewriterEffect).GetField("audioSource", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (audioSourceField != null)
        {
            audioSourceField.SetValue(currentTypewriter, typewriterAudioSource);
        }
        
        var typeSoundField = typeof(TypewriterEffect).GetField("typeSound", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (typeSoundField != null && typewriterSound != null)
        {
            typeSoundField.SetValue(currentTypewriter, typewriterSound);
        }
        
        var minPitchField = typeof(TypewriterEffect).GetField("minPitch", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (minPitchField != null)
        {
            minPitchField.SetValue(currentTypewriter, typewriterMinPitch);
        }
        
        var maxPitchField = typeof(TypewriterEffect).GetField("maxPitch", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (maxPitchField != null)
        {
            maxPitchField.SetValue(currentTypewriter, typewriterMaxPitch);
        }
        currentTypewriter.SetText(step.textToType);
        currentTypewriter.StartTyping();

        if (step.waitForCompletion)
        {
            yield return new WaitUntil(() => !currentTypewriter.IsTyping);
        }

        MoveToNextStep();
    }

    private IEnumerator HandleWaitStep(float duration)
    {
        yield return useUnscaledTime ? new WaitForSecondsRealtime(duration) : new WaitForSeconds(duration);
        MoveToNextStep();
    }

    private IEnumerator HandleWaitForInputStep()
    {
        isWaitingForInput = true;
        while (isWaitingForInput)
        {
            yield return null;
        }
        MoveToNextStep();
    }

    private IEnumerator HandleUIAnimationStep(SequenceStep step)
    {
        if (step.targetPanel == null)
        {
            Debug.LogWarning("No target panel assigned for UI animation step!");
            MoveToNextStep();
            yield break;
        }

        switch (step.uiAnimationType)
        {
            case UIAnimationType.Scroll:
                if (step.waitForCompletion)
                {
                    yield return StartCoroutine(AnimatePanelScroll(
                        step.targetPanel,
                        step.startPosition,
                        step.endPosition,
                        step.animationDuration,
                        step.animationCurve
                    ));
                }
                else
                {
                    // Start the animation but don't wait for it to complete
                    StartCoroutine(AnimatePanelScroll(
                        step.targetPanel,
                        step.startPosition,
                        step.endPosition,
                        step.animationDuration,
                        step.animationCurve
                    ));
                }
                break;
        }

        MoveToNextStep();
    }

    private IEnumerator AnimatePanelScroll(RectTransform panel, Vector2 startPos, Vector2 endPos, float duration, AnimationCurve curve)
    {
        float elapsed = 0f;
        panel.anchoredPosition = startPos;

        while (elapsed < duration)
        {
            float t = curve.Evaluate(elapsed / duration);
            panel.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        panel.anchoredPosition = endPos;
    }

    private void HandleSetActiveStep(SequenceStep step)
    {
        if (step.targetObject != null)
        {
            step.targetObject.SetActive(step.setActiveState);
        }
        else
        {
            Debug.LogWarning("No target object assigned for SetActive step!");
        }
        
        MoveToNextStep();
    }

    private void MoveToNextStep()
    {
        if (isSequenceRunning)
        {
            currentStepIndex++;
            if (currentStepIndex < sequenceSteps.Length)
            {
                ProcessCurrentStep();
            }
            else if (loopSequence)
            {
                currentStepIndex = 0;
                ProcessCurrentStep();
            }
            else
            {
                isSequenceRunning = false;
            }
        }
    }

    private void CheckAndPerformSkip()
    {
        if (currentStepIndex < sequenceSteps.Length)
        {
            SequenceStep currentStep = sequenceSteps[currentStepIndex];
            
            if (currentStep.enableSkip && currentStep.skipToIndex >= 0 && currentStep.skipToIndex < sequenceSteps.Length)
            {
                SkipToStep(currentStep.skipToIndex);
            }
        }
    }

    public void SkipToStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= sequenceSteps.Length)
        {
            return;
        }

        // Stop any current typewriter effect
        if (currentTypewriter != null)
        {
            currentTypewriter.StopAllCoroutines();
            currentTypewriter = null;
        }

        // Stop any running coroutines
        StopAllCoroutines();
        
        // Reset waiting state
        isWaitingForInput = false;
        
        // Play all steps before the target index immediately
        for (int i = 0; i < stepIndex; i++)
        {
            ExecuteStepImmediately(sequenceSteps[i]);
        }
        
        // Set the new step index and process
        currentStepIndex = stepIndex;
        ProcessCurrentStep();
    }

    private void ExecuteStepImmediately(SequenceStep step)
    {
        switch (step.actionType)
        {
            case SequenceActionType.TypeText:
                if (step.targetText != null)
                {
                    step.targetText.text = step.textToType;
                }
                break;

            case SequenceActionType.Wait:
                // Skip wait steps entirely
                break;

            case SequenceActionType.WaitForInput:
                // Skip wait for input steps entirely
                break;

            case SequenceActionType.UIAnimation:
                if (step.targetPanel != null && step.uiAnimationType == UIAnimationType.Scroll)
                {
                    step.targetPanel.anchoredPosition = step.endPosition;
                }
                break;

            case SequenceActionType.SetActive:
                if (step.targetObject != null)
                {
                    step.targetObject.SetActive(step.setActiveState);
                }
                break;
        }
    }
}