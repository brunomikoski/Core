using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.Events
{
    public class SafeEventHierarchyGroup : SafeEventHierarchyNode
    {
        public readonly List<SafeEventHierarchyNode> children = new List<SafeEventHierarchyNode>();
        public bool foldout = true;
        public Object unityObject;
        public Object UnityObject
        {
            get
            {
                if (unityObject != null)
                    return unityObject;

                if (parent != null)
                    return parent.UnityObject;

                return unityObject;
            }
        }

        public List<SafeSafeEventHierarchySafeEvent> ToList()
        {
            List<SafeSafeEventHierarchySafeEvent> result = new List<SafeSafeEventHierarchySafeEvent>();

            Queue<SafeEventHierarchyNode> queue = new Queue<SafeEventHierarchyNode>();

            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                SafeEventHierarchyNode node = queue.Dequeue();
                SafeSafeEventHierarchySafeEvent safeSafeEventNode = node as SafeSafeEventHierarchySafeEvent;
                if (safeSafeEventNode != null)
                {
                    result.Add(safeSafeEventNode);
                    continue;
                }

                SafeEventHierarchyGroup safeEventGroup = node as SafeEventHierarchyGroup;

                if (safeEventGroup != null)
                {
                    foreach (SafeEventHierarchyNode eventHierarchyNode in safeEventGroup.children)
                        queue.Enqueue(eventHierarchyNode);
                }
            }
            return result;
        }
    }
}
