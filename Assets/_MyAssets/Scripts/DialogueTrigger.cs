using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class RuntimeDialogueStep
{
    [HideInInspector]
    public bool isExpanded = true;

    [Tooltip("Custom name for this step (for readability)")]
    public string stepName = "Step";
    public DialogueStepType stepType = DialogueStepType.Text;

    [TextArea]
    public List<string> textLines = new List<string>();

    public UnityEvent onEvent;
    public bool waitForEventComplete = true;

    [Tooltip("Duration in seconds for Wait step type")]
    public float waitDuration = 1.0f;
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Data")]
    public DialogueData dialogueTemplate;
    public bool lockPlayer = true;
    public bool unlockPlayerOnEnd = true;

    [Header("Runtime Steps (overrides template if not empty)")]
    public List<RuntimeDialogueStep> runtimeSteps = new List<RuntimeDialogueStep>();

    public void TriggerDialogue()
    {
        // Create runtime dialogue from template or use runtime steps directly
        DialogueData runtimeData = CreateRuntimeDialogue();
        if (runtimeData != null)
        {
            DialogueManager.Instance.StartDialogue(runtimeData, lockPlayer);
        }
    }

    private DialogueData CreateRuntimeDialogue()
    {
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lockPlayer = lockPlayer;
        data.unlockPlayerOnEnd = unlockPlayerOnEnd;
        data.steps = new List<DialogueStep>();

        // If runtime steps are defined, use those (allows scene references)
        if (runtimeSteps.Count > 0)
        {
            foreach (RuntimeDialogueStep runtimeStep in runtimeSteps)
            {
                DialogueStep step = new DialogueStep
                {
                    isExpanded = runtimeStep.isExpanded,
                    stepName = runtimeStep.stepName,
                    stepType = runtimeStep.stepType,
                    textLines = new List<string>(runtimeStep.textLines),
                    onEvent = runtimeStep.onEvent,
                    waitForEventComplete = runtimeStep.waitForEventComplete,
                    waitDuration = runtimeStep.waitDuration
                };
                data.steps.Add(step);
            }
        }
        // Otherwise copy from template
        else if (dialogueTemplate != null)
        {
            foreach (DialogueStep templateStep in dialogueTemplate.steps)
            {
                DialogueStep step = new DialogueStep
                {
                    isExpanded = templateStep.isExpanded,
                    stepName = templateStep.stepName,
                    stepType = templateStep.stepType,
                    textLines = new List<string>(templateStep.textLines),
                    onEvent = new UnityEvent(),
                    waitForEventComplete = templateStep.waitForEventComplete,
                    waitDuration = templateStep.waitDuration
                };
                data.steps.Add(step);
            }
        }

        return data.steps.Count > 0 ? data : null;
    }
}
