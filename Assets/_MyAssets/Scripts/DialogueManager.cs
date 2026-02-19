using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    [Header("UI")]
    public TextMeshProUGUI dialogueTMP;
    public CanvasGroup dialogueCanvasGroup, cutsceneCanvasGroup, playerUICanvasGroup;
    public float fadeDuration = 0.2f;
    public float revealInterval = 0.02f;
    public bool IsDialogueActive => _isDialogueActive;

    [SerializeField] bool isDebugMode = false;

    [Header("Input")]
#if ENABLE_INPUT_SYSTEM
    public InputActionReference advanceAction;
#endif

    [Header("Testing")]
    [SerializeField] DialogueTrigger testDialogueTrigger;

    [SerializeField] FirstPersonController playerController;

    private Coroutine _dialogueRoutine;
    private Coroutine _revealRoutine;
    private DialogueData _currentDialogue;
    private int _currentIndex;
    private bool _isDialogueActive;
    private bool _isRevealing;
    private bool _skipReveal;
    private bool _advanceRequested;
    private bool _waitingForEvent;
    private bool _lockPlayer;
    private bool _unlockPlayerOnEnd;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        if (advanceAction != null && advanceAction.action != null)
        {
            advanceAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (advanceAction != null && advanceAction.action != null)
        {
            advanceAction.action.Disable();
        }
    }
#endif

    private void Update()
    {
        // Test dialogue with T key
        if (Keyboard.current.tKey.wasPressedThisFrame && testDialogueTrigger != null && !_isDialogueActive && isDebugMode)
        {
            testDialogueTrigger.TriggerDialogue();
        }

        if (!_isDialogueActive)
        {
            return;
        }

        if (WasAdvancePressed())
        {
            HandleAdvanceInput();
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null)
        {
            return;
        }

        StartDialogue(dialogue, dialogue.lockPlayer);
    }

    public void StartDialogue(DialogueData dialogue, bool lockPlayer)
    {
        if (dialogue == null)
        {
            return;
        }

        StopDialogue();
        _currentDialogue = dialogue;
        _currentIndex = 0;
        _lockPlayer = lockPlayer;
        _unlockPlayerOnEnd = dialogue.unlockPlayerOnEnd;
        _dialogueRoutine = StartCoroutine(RunDialogue());
    }

    public void StopDialogue()
    {
        if (_dialogueRoutine != null)
        {
            StopCoroutine(_dialogueRoutine);
            _dialogueRoutine = null;
        }
        if (_revealRoutine != null)
        {
            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }

        _isDialogueActive = false;
        _isRevealing = false;
        _skipReveal = false;
        _advanceRequested = false;
        _waitingForEvent = false;

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0.0f;
            dialogueCanvasGroup.blocksRaycasts = false;
            dialogueCanvasGroup.interactable = false;
        }

        SetPlayerLock(false);
    }

    public void CompleteDialogueEvent()
    {
        _waitingForEvent = false;
    }

    public void UnlockPlayer()
    {
        SetPlayerLock(false);
    }

    public void LockPlayer()
    {
        SetPlayerLock(true);
    }

    private IEnumerator RunDialogue()
    {
        _isDialogueActive = true;
        _advanceRequested = false;
        _waitingForEvent = false;
        _skipReveal = false;

        SetPlayerLock(_lockPlayer);

        // Start with dialogue canvas at zero alpha
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0.0f;
        }

        // Fade out player UI and fade in cutscene canvas at the start
        if (playerUICanvasGroup != null)
        {
            yield return FadePlayerUICanvas(0.0f, fadeDuration);
        }

        if (cutsceneCanvasGroup != null)
        {
            yield return FadeCutsceneCanvas(1.0f, fadeDuration);
        }

        while (_currentDialogue != null && _currentIndex < _currentDialogue.steps.Count)
        {
            DialogueStep step = _currentDialogue.steps[_currentIndex];
            Debug.Log($"[Dialogue] Triggering step: {step.stepName} ({step.stepType})", this);
            _advanceRequested = false;

            if (step.stepType == DialogueStepType.Text)
            {
                if (step.textLines != null && step.textLines.Count > 0)
                {
                    // Fade in dialogue canvas before showing text
                    if (dialogueCanvasGroup != null && dialogueCanvasGroup.alpha < 0.5f)
                    {
                        dialogueCanvasGroup.blocksRaycasts = true;
                        dialogueCanvasGroup.interactable = true;
                        yield return FadeCanvas(1.0f, fadeDuration);
                    }

                    foreach (string textLine in step.textLines)
                    {
                        string text = textLine ?? string.Empty;
                        _revealRoutine = StartCoroutine(RevealText(text));
                        while (_isRevealing)
                        {
                            yield return null;
                        }

                        _advanceRequested = false;
                        while (!_advanceRequested)
                        {
                            yield return null;
                        }
                    }

                    // Fade out dialogue canvas after text step is complete
                    if (dialogueCanvasGroup != null)
                    {
                        yield return FadeCanvas(0.0f, fadeDuration);
                        // Clear text after fading out
                        if (dialogueTMP != null)
                        {
                            dialogueTMP.text = string.Empty;
                        }
                    }
                }
            }
            else if (step.stepType == DialogueStepType.Event)
            {
                _waitingForEvent = step.waitForEventComplete;
                step.onEvent?.Invoke();
                while (_waitingForEvent)
                {
                    yield return null;
                }
            }
            else if (step.stepType == DialogueStepType.Wait)
            {
                yield return new WaitForSeconds(step.waitDuration);
            }

            _currentIndex++;
        }

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0.0f;
            dialogueCanvasGroup.blocksRaycasts = false;
            dialogueCanvasGroup.interactable = false;
        }

        // Fade out cutscene canvas and fade in player UI at the end
        if (cutsceneCanvasGroup != null)
        {
            yield return FadeCutsceneCanvas(0.0f, fadeDuration);
        }

        if (playerUICanvasGroup != null)
        {
            yield return FadePlayerUICanvas(1.0f, fadeDuration);
        }

        if (_unlockPlayerOnEnd)
        {
            SetPlayerLock(false);
        }
        _isDialogueActive = false;
    }

    private IEnumerator RevealText(string text)
    {
        _isRevealing = true;
        _skipReveal = false;

        if (dialogueTMP == null)
        {
            _isRevealing = false;
            yield break;
        }

        dialogueTMP.text = text;
        dialogueTMP.maxVisibleCharacters = 0;
        dialogueTMP.ForceMeshUpdate();
        int totalCharacters = dialogueTMP.textInfo.characterCount;

        for (int i = 0; i <= totalCharacters; i++)
        {
            if (_skipReveal)
            {
                break;
            }
            dialogueTMP.maxVisibleCharacters = i;
            if (revealInterval > 0.0f)
            {
                yield return new WaitForSeconds(revealInterval);
            }
            else
            {
                yield return null;
            }
        }

        dialogueTMP.maxVisibleCharacters = totalCharacters;
        _isRevealing = false;
    }

    private void HandleAdvanceInput()
    {
        if (_waitingForEvent)
        {
            return;
        }

        if (_isRevealing)
        {
            _skipReveal = true;
            return;
        }

        _advanceRequested = true;
    }

    private bool WasAdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (advanceAction != null && advanceAction.action != null)
        {
            return advanceAction.action.WasPressedThisFrame();
        }
#endif
        return Input.GetMouseButtonDown(0);
    }

    private IEnumerator FadeCanvas(float targetAlpha, float duration)
    {
        if (dialogueCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = dialogueCanvasGroup.alpha;
        float timer = 0.0f;
        if (duration <= 0.0f)
        {
            dialogueCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            dialogueCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        dialogueCanvasGroup.alpha = targetAlpha;
    }

    private IEnumerator FadeCutsceneCanvas(float targetAlpha, float duration)
    {
        if (cutsceneCanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = cutsceneCanvasGroup.alpha;
        float timer = 0.0f;
        if (duration <= 0.0f)
        {
            cutsceneCanvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            cutsceneCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        cutsceneCanvasGroup.alpha = targetAlpha;
    }

    private IEnumerator FadePlayerUICanvas(float targetAlpha, float duration)
    {
        if (playerUICanvasGroup == null)
        {
            yield break;
        }

        float startAlpha = playerUICanvasGroup.alpha;
        float timer = 0.0f;
        if (duration <= 0.0f)
        {
            playerUICanvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            playerUICanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        playerUICanvasGroup.alpha = targetAlpha;
    }

    private void SetPlayerLock(bool locked)
    {
        if (playerController == null)
        {
            return;
        }

        playerController.canLook = !locked;
        playerController.canMove = !locked;
        
        if (!locked)
        {
            playerController.isForceLooking = false;
        }
    }
    
}
