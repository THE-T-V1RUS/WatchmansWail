using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using System;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InteractionController : MonoBehaviour
{
    public event System.Action<Interactable> OnInteractableEncountered;
    public event System.Action OnInteractionTriggered;

    [Header("Interaction")]
    [Tooltip("Maximum distance to check for interactables")]
    public float MaxDistance = 3.0f;
    [Tooltip("Only objects with this tag are considered interactable")]
    public string InteractableTag = "Interactable";
    [Tooltip("Layers to include when raycasting")]
    public LayerMask InteractionLayers = ~0;

    public GameObject CurrentInteractable { get; private set; }
    public Interactable CurrentInteractableComponent { get; private set; }

    [SerializeField] Image crosshairImg;
    [SerializeField] Sprite normalCrosshair, interactCrosshair;
    [SerializeField] TextMeshProUGUI interactTextLabel;
#if ENABLE_INPUT_SYSTEM
    [SerializeField] InputActionReference interactAction;
#endif

    private StarterAssetsInputs _input;
    private Interactable _lastEncounteredInteractable;

    void Start()
    {
        _input = GetComponentInParent<StarterAssetsInputs>();
        if (_input == null)
        {
            Debug.LogWarning("InteractionController: Could not find StarterAssetsInputs component on parent.");
        }
#if ENABLE_INPUT_SYSTEM
        if (interactAction == null || interactAction.action == null)
        {
            Debug.LogWarning("InteractionController: Interact Action is not assigned in the inspector.");
        }
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null && interactAction.action != null)
        {
            interactAction.action.Disable();
        }
    }
#endif

    void Update()
    {
        UpdateInteractable();
    }

    private void UpdateInteractable()
    {
        // Block interactions during dialogue/cutscenes
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            crosshairImg.sprite = normalCrosshair;
            CurrentInteractable = null;
            CurrentInteractableComponent = null;
            _lastEncounteredInteractable = null;
            if (interactTextLabel != null)
            {
                interactTextLabel.text = string.Empty;
            }
            return;
        }

        crosshairImg.sprite = normalCrosshair;
        CurrentInteractable = null;
        CurrentInteractableComponent = null;
        Interactable encounteredThisFrame = null;
        bool interactPressed = false;
    #if ENABLE_INPUT_SYSTEM
        if (interactAction != null && interactAction.action != null)
        {
            interactPressed = interactAction.action.WasPressedThisFrame();
        }
        else if (_input != null)
        {
            interactPressed = _input.ConsumeInteractPressed();
        }
    #else
        if (_input != null)
        {
            interactPressed = _input.ConsumeInteractPressed();
        }
    #endif
        if (interactTextLabel != null)
        {
            interactTextLabel.text = string.Empty;
        }

        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, MaxDistance, InteractionLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag(InteractableTag))
            {
                CurrentInteractable = hit.collider.gameObject;
                CurrentInteractableComponent = hit.collider.GetComponent<Interactable>();
                if (CurrentInteractableComponent != null && CurrentInteractableComponent.isActiveAndEnabled)
                {
                    encounteredThisFrame = CurrentInteractableComponent;
                    crosshairImg.sprite = interactCrosshair;
                    if (interactTextLabel != null)
                    {
                        try
                        {
                            interactTextLabel.text = CurrentInteractableComponent.interactText;
                        }
                        catch (MissingReferenceException)
                        {
                            CurrentInteractable = null;
                            CurrentInteractableComponent = null;
                            interactTextLabel.text = string.Empty;
                            return;
                        }
                    }
                    if (interactPressed)
                    {
                        try
                        {
                            CurrentInteractableComponent.Interact();
                        }
                        catch (MissingReferenceException ex)
                        {
                            Debug.LogWarning($"InteractionController: Interactable was destroyed during interaction. {ex.Message}", this);
                            CurrentInteractable = null;
                            CurrentInteractableComponent = null;
                            return;
                        }

                        try
                        {
                            OnInteractionTriggered?.Invoke();
                        }
                        catch (MissingReferenceException ex)
                        {
                            Debug.LogWarning($"InteractionController: A listener on OnInteractionTriggered referenced a destroyed object. Owner='{name}'. {ex.Message}", this);
                        }
                    }
                }
            }
        }

        if (encounteredThisFrame != null && encounteredThisFrame != _lastEncounteredInteractable)
        {
            _lastEncounteredInteractable = encounteredThisFrame;
            try
            {
                OnInteractableEncountered?.Invoke(encounteredThisFrame);
            }
            catch (MissingReferenceException ex)
            {
                Debug.LogWarning($"InteractionController: A listener on OnInteractableEncountered referenced a destroyed object. {ex.Message}", this);
            }
        }
        else if (encounteredThisFrame == null)
        {
            _lastEncounteredInteractable = null;
        }
    }
}
