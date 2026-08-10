using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.SafeEvents
{
    public class SafeSafeEventsesHierarchyGroup : SafeEventsHierarchyNode
    {
        public readonly List<SafeEventsHierarchyNode> children = new List<SafeEventsHierarchyNode>();
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

        public List<SafeSafeEventsesHierarchySafeEvents> ToList()
        {
            List<SafeSafeEventsesHierarchySafeEvents> result = new List<SafeSafeEventsesHierarchySafeEvents>();

            Queue<SafeEventsHierarchyNode> queue = new Queue<SafeEventsHierarchyNode>();

            queue.Enqueue(this);

            while (queue.Count > 0)
            {
                SafeEventsHierarchyNode node = queue.Dequeue();
                SafeSafeEventsesHierarchySafeEvents safeSafeEventsesHierarchySafeEventsNode = node as SafeSafeEventsesHierarchySafeEvents;
                if (safeSafeEventsesHierarchySafeEventsNode != null)
                {
                    result.Add(safeSafeEventsesHierarchySafeEventsNode);
                    continue;
                }

                SafeSafeEventsesHierarchyGroup safeSafeEventsesGroup = node as SafeSafeEventsesHierarchyGroup;

                if (safeSafeEventsesGroup != null)
                {
                    foreach (SafeEventsHierarchyNode eventHierarchyNode in safeSafeEventsesGroup.children)
                        queue.Enqueue(eventHierarchyNode);
                }
            }
            return result;
        }
    }
}
