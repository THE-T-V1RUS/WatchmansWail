using UnityEngine;

public class PipeOrganLogic : MonoBehaviour
{
    [SerializeField] private SonarMinigameController sonarController;
    [SerializeField] private DialogueTrigger turnOnMonitorDialogueTrigger, turnOnSonarDialogueTrigger, startMinigameDialogueTrigger;

    public void ActivateSonarMinigame()
    {
        if (sonarController == null) return;

        if (!sonarController.MonitorIsActive())
        {
            turnOnMonitorDialogueTrigger.TriggerDialogue();
            return;
        }

        if (!sonarController.SonarIsActive())
        {
            turnOnSonarDialogueTrigger.TriggerDialogue();
            return;
        }
        
        startMinigameDialogueTrigger.TriggerDialogue();
    }
}
