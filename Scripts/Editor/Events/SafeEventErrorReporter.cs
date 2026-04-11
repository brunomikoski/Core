using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BrunoMikoski.Core.Events
{
    public static class SafeEventErrorReporter
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                CheckSubscriptions();
            }
        }

        private static void CheckSubscriptions()
        {
            SafeEventHierarchyGroup root = new SafeEventHierarchyGroup();
            SafeEventsFinder.FindEvents(root);

            List<SafeSafeEventHierarchySafeEvent> list = root.ToList();
            foreach (SafeSafeEventHierarchySafeEvent eventNode in list)
            {
                if (eventNode.eventBase.HasListeners)
                {
                    Object eventNodeUnityObject = eventNode.UnityObject;

                    foreach (SafeEventBase.CallbackData eventBaseListener in eventNode.eventBase.Listeners)
                    {
                        MonoBehaviour monoBehaviour = eventBaseListener.target as MonoBehaviour;

                        if (monoBehaviour != null && monoBehaviour.runInEditMode)
                            continue;

                        Debug.LogError(
                            $"Event still has listeners: {eventNode} " +
                            $"{eventBaseListener.target.GetType()}", eventNodeUnityObject);
                    }
                }
            }
        }
    }
}
