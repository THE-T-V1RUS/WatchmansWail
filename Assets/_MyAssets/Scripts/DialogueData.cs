using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum DialogueStepType
{
    Text,
    Event,
    Wait
}

[System.Serializable]
public class DialogueStep
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

[CreateAssetMenu(menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public bool lockPlayer = true;
    public bool unlockPlayerOnEnd = true;
    public List<DialogueStep> steps = new List<DialogueStep>();
}
