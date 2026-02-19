using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(RuntimeDialogueStep))]
public class RuntimeDialogueStepDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty isExpandedProperty = property.FindPropertyRelative("isExpanded");
        bool isExpanded = isExpandedProperty.boolValue;

        // Always include header
        float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        if (!isExpanded)
            return height;

        // When expanded, add all the fields
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Step name
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Step type

        SerializedProperty stepTypeProperty = property.FindPropertyRelative("stepType");
        DialogueStepType stepType = (DialogueStepType)stepTypeProperty.enumValueIndex;

        if (stepType == DialogueStepType.Text)
        {
            SerializedProperty textLinesProperty = property.FindPropertyRelative("textLines");
            height += EditorGUI.GetPropertyHeight(textLinesProperty) + EditorGUIUtility.standardVerticalSpacing;
        }
        else if (stepType == DialogueStepType.Event)
        {
            SerializedProperty onEventProperty = property.FindPropertyRelative("onEvent");
            SerializedProperty waitForEventProperty = property.FindPropertyRelative("waitForEventComplete");
            height += EditorGUI.GetPropertyHeight(onEventProperty) + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
        else if (stepType == DialogueStepType.Wait)
        {
            SerializedProperty waitDurationProperty = property.FindPropertyRelative("waitDuration");
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty isExpandedProperty = property.FindPropertyRelative("isExpanded");
        SerializedProperty stepNameProperty = property.FindPropertyRelative("stepName");
        SerializedProperty stepTypeProperty = property.FindPropertyRelative("stepType");

        // Draw foldout header
        Rect headerRect = position;
        headerRect.height = EditorGUIUtility.singleLineHeight;

        string headerLabel = stepNameProperty.stringValue;
        if (string.IsNullOrEmpty(headerLabel))
            headerLabel = "Step";

        isExpandedProperty.boolValue = EditorGUI.Foldout(headerRect, isExpandedProperty.boolValue, headerLabel);

        if (!isExpandedProperty.boolValue)
        {
            EditorGUI.EndProperty();
            return;
        }

        // Draw expanded content
        float yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Step Name
        Rect stepNameRect = position;
        stepNameRect.y += yOffset;
        stepNameRect.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(stepNameRect, stepNameProperty, new GUIContent("Step Name"));

        yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Step Type
        Rect stepTypeRect = position;
        stepTypeRect.y += yOffset;
        stepTypeRect.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(stepTypeRect, stepTypeProperty, new GUIContent("Step Type"));

        yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        DialogueStepType stepType = (DialogueStepType)stepTypeProperty.enumValueIndex;

        if (stepType == DialogueStepType.Text)
        {
            Rect textLinesRect = position;
            textLinesRect.y += yOffset;
            textLinesRect.height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("textLines"));

            SerializedProperty textLinesProperty = property.FindPropertyRelative("textLines");
            EditorGUI.PropertyField(textLinesRect, textLinesProperty, new GUIContent("Text Lines"));
        }
        else if (stepType == DialogueStepType.Event)
        {
            SerializedProperty onEventProperty = property.FindPropertyRelative("onEvent");
            float eventHeight = EditorGUI.GetPropertyHeight(onEventProperty);

            Rect eventRect = position;
            eventRect.y += yOffset;
            eventRect.height = eventHeight;

            EditorGUI.PropertyField(eventRect, onEventProperty, new GUIContent("On Event"));

            yOffset += eventHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect waitRect = position;
            waitRect.y += yOffset;
            waitRect.height = EditorGUIUtility.singleLineHeight;

            SerializedProperty waitForEventProperty = property.FindPropertyRelative("waitForEventComplete");
            EditorGUI.PropertyField(waitRect, waitForEventProperty, new GUIContent("Wait For Complete"));
        }
        else if (stepType == DialogueStepType.Wait)
        {
            Rect waitDurationRect = position;
            waitDurationRect.y += yOffset;
            waitDurationRect.height = EditorGUIUtility.singleLineHeight;

            SerializedProperty waitDurationProperty = property.FindPropertyRelative("waitDuration");
            EditorGUI.PropertyField(waitDurationRect, waitDurationProperty, new GUIContent("Wait Duration"));
        }

        EditorGUI.EndProperty();
    }
}
#endif
