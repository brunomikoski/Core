using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Trigger), true)]
    public sealed class TriggerCustomEditor : ReferencedByCustomEditor
    {
        protected override void DrawWarnings(Transform root, IReadOnlyList<ReferencingProperty> found)
        {
            Trigger trigger = (Trigger)target;
            int emptyCount = CountEmptyEntries(trigger.Triggerables);
            if (emptyCount == 0)
                return;

            EditorGUILayout.HelpBox($"Empty entries in the Trigger List: {emptyCount}.", MessageType.Warning);
        }

        private static int CountEmptyEntries(List<TriggerData> triggerDatas)
        {
            int count = 0;
            for (int i = 0; i < triggerDatas.Count; i++)
            {
                if (triggerDatas[i].Triggerable == null)
                    count++;
            }

            return count;
        }
    }
}
