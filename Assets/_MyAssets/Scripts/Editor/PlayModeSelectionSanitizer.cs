#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Prevents MissingReferenceException from the Inspector when stopping play mode.
/// The Inspector caches Editor objects whose targets (Interactable, MonitorButton)
/// get destroyed during the play→edit transition. Unity's RedrawFromNative() then
/// accesses .enabled on the destroyed targets before any managed callback can clean up.
///
/// Fix: During ExitingPlayMode (before destruction), hide the components from the
/// Inspector so the cached EditorElements skip their header draw entirely.
/// </summary>
[InitializeOnLoad]
public static class PlayModeSelectionSanitizer
{
    private static System.Type _inspectorWindowType;
    private static PropertyInfo _trackerProperty;

    static PlayModeSelectionSanitizer()
    {
        // Cache reflection lookups for InspectorWindow.tracker
        _inspectorWindowType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        if (_inspectorWindowType != null)
        {
            _trackerProperty = _inspectorWindowType.GetProperty(
                "tracker",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        }

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // BEFORE objects are destroyed: hide vulnerable components from Inspector
            // so the stale EditorElement header draw skips them entirely.
            HideRuntimeComponents<Interactable>();
            HideRuntimeComponents<MonitorButton>();

            // Clear selection and rebuild all trackers while objects still exist.
            ClearSelectionAndRebuildAllTrackers();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            // After scene reload: rebuild trackers one more time to pick up fresh objects.
            ClearSelectionAndRebuildAllTrackers();
        }
    }

    private static void HideRuntimeComponents<T>() where T : Component
    {
        var components = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (var component in components)
        {
            if (component != null)
            {
                component.hideFlags |= HideFlags.HideInInspector;
            }
        }
    }

    private static void ClearSelectionAndRebuildAllTrackers()
    {
        // Clear all forms of selection.
        Selection.objects = System.Array.Empty<Object>();
        Selection.activeObject = null;
        Selection.activeGameObject = null;

        // Rebuild the shared tracker.
        if (ActiveEditorTracker.sharedTracker != null)
        {
            ActiveEditorTracker.sharedTracker.isLocked = false;
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        // Find ALL open Inspector windows and rebuild each one's own tracker.
        if (_inspectorWindowType != null)
        {
            var windows = Resources.FindObjectsOfTypeAll(_inspectorWindowType);
            foreach (var window in windows)
            {
                if (_trackerProperty != null)
                {
                    var tracker = _trackerProperty.GetValue(window) as ActiveEditorTracker;
                    if (tracker != null)
                    {
                        tracker.isLocked = false;
                        tracker.ForceRebuild();
                    }
                }

                ((EditorWindow)window).Repaint();
            }
        }

        InternalEditorUtility.RepaintAllViews();
    }
}
#endif
