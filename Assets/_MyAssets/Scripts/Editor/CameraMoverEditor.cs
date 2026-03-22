using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraMover))]
public class CameraMoverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
        {
            return;
        }

        CameraMover mover = (CameraMover)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Move To Destination", GUILayout.Height(30)))
            {
                mover.MoveToDestination();
            }

            if (GUILayout.Button("Return To Player", GUILayout.Height(30)))
            {
                mover.ReturnToPlayer();
            }
        }
    }
}
