using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class InteractionController : MonoBehaviour
{
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
            if (interactTextLabel != null)
            {
                interactTextLabel.text = string.Empty;
            }
            return;
        }

        crosshairImg.sprite = normalCrosshair;
        CurrentInteractable = null;
        CurrentInteractableComponent = null;
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
                if (CurrentInteractableComponent != null)
                {
                    crosshairImg.sprite = interactCrosshair;
                    if (interactTextLabel != null)
                    {
                        interactTextLabel.text = CurrentInteractableComponent.interactText;
                    }
                    if (interactPressed)
                    {
                        CurrentInteractableComponent.Interact();
                    }
                }
            }
        }
    }
}
