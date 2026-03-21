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

    public void ButtonPressed()
    {
        if (minigameController != null && buttonInteractable != null)
        {
            AudioManager.Instance.PlaySfxSimple(buttonPressSound);
            buttonAnimator.SetTrigger("Press");
            interactionCollider.enabled = false;
            StartCoroutine(reenableColliderAfterDelay(0.5f));

            if (minigameController.MonitorIsActive())
            {
                minigameController.SetScreenOn(false);
                buttonInteractable.SetInteractText("Turn Monitor On");
            }
            else
            {
                minigameController.SetScreenOn(true);
                buttonInteractable.SetInteractText("Turn Monitor Off");
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
            buttonAnimator.SetBool("Switch", !buttonAnimator.GetBool("Switch"));
            interactionCollider.enabled = false;
            StartCoroutine(reenableColliderAfterDelay(0.5f));

            if (sonarPingController.PingEnabled)
            {
                sonarPingController.PingEnabled = false;
                buttonInteractable.SetInteractText("Enable Sonar");
            }
            else
            {
                sonarPingController.PingEnabled = true;
                buttonInteractable.SetInteractText("Disable Sonar");
            }
        }
        else
        {
            Debug.LogWarning("SonarPingController or ButtonInteractable is not assigned.");
        }
    }

    IEnumerator reenableColliderAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        interactionCollider.enabled = true;
    }   
}
