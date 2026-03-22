using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [TextArea]
    public string interactText = "";

    [Tooltip("Invoked when the player interacts with this object")]
    public UnityEvent onInteract;

    public void Interact()
    {
        if (onInteract == null)
        {
            return;
        }

        try
        {
            onInteract.Invoke();
        }
        catch (MissingReferenceException ex)
        {
            Debug.LogWarning($"Interactable '{name}' has a destroyed listener target: {ex.Message}", this);
        }
    }

#if UNITY_EDITOR
    private void OnDestroy()
    {
        // Prevent Inspector MissingReferenceException by clearing selection
        // before the editor tries to redraw a destroyed component header.
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            UnityEditor.Selection.activeGameObject = null;
        }
    }
#endif

    public void SetInteractText(string newText)
    {
        interactText = newText;
    }
}
