using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(npcController))]
public class npcControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (!Application.isPlaying)
            return;

        npcController npc = (npcController)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Walk To Destination", GUILayout.Height(30)))
        {
            npc.MoveToDestination();
        }
    }
}
