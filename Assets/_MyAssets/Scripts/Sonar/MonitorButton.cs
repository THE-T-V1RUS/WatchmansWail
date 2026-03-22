using System.Collections;
using UnityEngine;

public class MonitorButton : MonoBehaviour
{
    public SonarMinigameController minigameController;
    public Interactable buttonInteractable;
    public AudioClip buttonPressSound;
    public Animator buttonAnimator;
    public SonarPingController sonarPingController;
    public BoxCollider interactionCollider;

    private void Awake()
    {
        if (buttonInteractable == null)
        {
            buttonInteractable = GetComponent<Interactable>();
        }
    }

    public void ButtonPressed()
    {
        if (minigameController != null && buttonInteractable != null)
        {
            AudioManager.Instance.PlaySfxSimple(buttonPressSound);
            if (buttonAnimator != null)
            {
                buttonAnimator.SetTrigger("Press");
            }

            if (interactionCollider != null)
            {
                interactionCollider.enabled = false;
            }

            StartCoroutine(reenableColliderAfterDelay(0.5f));

            if (minigameController.MonitorIsActive())
            {
                minigameController.SetScreenOn(false);
                if (buttonInteractable != null)
                {
                    buttonInteractable.SetInteractText("Turn Monitor On");
                }
            }
            else
            {
                minigameController.SetScreenOn(true);
                if (buttonInteractable != null)
                {
                    buttonInteractable.SetInteractText("Turn Monitor Off");
                }
            }
        }
        else
        {
            Debug.LogWarning("MinigameController or ButtonInteractable is not assigned.");
        }
    }

    public void SwitchFlipped()
    {
        if (sonarPingController != null && buttonInteractable != null)
        {
            AudioManager.Instance.PlaySfxSimple(buttonPressSound);
            if (buttonAnimator != null)
            {
                buttonAnimator.SetBool("Switch", !buttonAnimator.GetBool("Switch"));
            }

            if (interactionCollider != null)
            {
                interactionCollider.enabled = false;
            }

            StartCoroutine(reenableColliderAfterDelay(0.5f));

            if (sonarPingController.PingEnabled)
            {
                sonarPingController.PingEnabled = false;
                if (buttonInteractable != null)
                {
                    buttonInteractable.SetInteractText("Enable Sonar");
                }
            }
            else
            {
                sonarPingController.PingEnabled = true;
                if (buttonInteractable != null)
                {
                    buttonInteractable.SetInteractText("Disable Sonar");
                }
            }
        }
        else
        {
            Debug.LogWarning("SonarPingController or ButtonInteractable is not assigned.");
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

    IEnumerator reenableColliderAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (this != null && interactionCollider != null)
        {
            interactionCollider.enabled = true;
        }
    }
}
