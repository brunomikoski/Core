using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Triggerable), true)]
    public sealed class TriggerableCustomEditor : ReferencedByCustomEditor
    {
        protected override void DrawWarnings(Transform root, IReadOnlyList<ReferencingProperty> found)
        {
            Triggerable triggerable = (Triggerable)target;
            if (triggerable.StartsActive)
                return;

            if (IsFiredByTrigger(found))
                return;

            EditorGUILayout.HelpBox(
                $"No Trigger under '{root.name}' fires this Triggerable. Add it to a Trigger List, or tick Starts Active.",
                MessageType.Warning);
        }

        private static bool IsFiredByTrigger(IReadOnlyList<ReferencingProperty> found)
        {
            for (int i = 0; i < found.Count; i++)
            {
                if (found[i].Owner is Trigger)
                    return true;
            }

            return false;
        }
    }
}
