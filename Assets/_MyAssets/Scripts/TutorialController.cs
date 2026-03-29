using UnityEngine;
using System.Collections;
using StarterAssets;

public class TutorialController : MonoBehaviour
{
    [SerializeField] CanvasGroup movementTutorial, interactionTutorial, steerShipTutorial;
    [SerializeField] StarterAssetsInputs playerInput;
    [SerializeField] InteractionController interactionController;

    const float lookThreshold = 0.01f;

    bool hasCompletedMovementTutorial = false;
    bool hasCompletedLookTutorial = false;
    bool playerHasShownMovementComprehension = false;
    bool hasDisplayedInteractionTutorial = false;
    bool hasShownInteractionComprehension = false;
    bool hasShownSteerShipComprehension = false;
    bool hasDisplayedSteerShipTutorial = false;

    void Awake()
    {
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<StarterAssetsInputs>();
        }

        if (interactionController == null)
        {
            interactionController = FindFirstObjectByType<InteractionController>();
        }
    }

    void OnEnable()
    {
        if (interactionController != null)
        {
            interactionController.OnInteractableEncountered += HandleInteractableEncountered;
            interactionController.OnInteractionTriggered += HandleInteractionTriggered;
        }
    }

    void OnDisable()
    {
        if (interactionController != null)
        {
            interactionController.OnInteractableEncountered -= HandleInteractableEncountered;
            interactionController.OnInteractionTriggered -= HandleInteractionTriggered;
        }
    }

    public void ShowTutorial(CanvasGroup tutorialCanvasGroup)
    {
        if (tutorialCanvasGroup == steerShipTutorial)
        {
            hasDisplayedSteerShipTutorial = true;
        }

        StartCoroutine(FadeInCanvasGroup(tutorialCanvasGroup, 1f));
    }

    public void HideTutorial(CanvasGroup tutorialCanvasGroup)
    {
        StartCoroutine(FadeOutCanvasGroup(tutorialCanvasGroup, 1f));
    }
    private IEnumerator FadeInCanvasGroup(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(true);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutCanvasGroup(CanvasGroup canvasGroup, float duration)
    {
        float elapsed = 0f;
        canvasGroup.alpha = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutCanvasGroupWithDelay(CanvasGroup canvasGroup, float duration, float delay = 0f)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float elapsed = 0f;
        canvasGroup.alpha = 1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    private void HandleInteractableEncountered(Interactable _)
    {
        if (hasDisplayedInteractionTutorial)
        {
            return;
        }

        hasDisplayedInteractionTutorial = true;
        ShowTutorial(interactionTutorial);
    }

    private void HandleInteractionTriggered()
    {
        if (hasShownInteractionComprehension)
        {
            return;
        }

        hasShownInteractionComprehension = true;
        StartCoroutine(FadeOutCanvasGroupWithDelay(interactionTutorial, 1f, 0f));
    }

    void Update()
    {
        if (playerInput == null)
        {
            return;
        }

        // If movement tutorial is active, check for comprehension
        if (movementTutorial.alpha > 0f && !playerHasShownMovementComprehension)
        {
            if (playerInput.move != Vector2.zero)
            {
                hasCompletedMovementTutorial = true;
            }
            
            if (playerInput.look.sqrMagnitude >= lookThreshold)
            {
                hasCompletedLookTutorial = true;
            }

            if (hasCompletedMovementTutorial && hasCompletedLookTutorial)
            {
                playerHasShownMovementComprehension = true;
                StartCoroutine(FadeOutCanvasGroupWithDelay(movementTutorial, 1f, 1f));
            }
        }

        // If steer ship tutorial is active, check for comprehension
        if (hasDisplayedSteerShipTutorial && !hasShownSteerShipComprehension)
        {
            if (Mathf.Abs(playerInput.move.x) > 0.1f)
            {
                hasShownSteerShipComprehension = true;
                StartCoroutine(FadeOutCanvasGroupWithDelay(steerShipTutorial, 1f, 1f));
            }
        }
    }
}
