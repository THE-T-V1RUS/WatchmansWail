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
        onInteract?.Invoke();
    }
}
