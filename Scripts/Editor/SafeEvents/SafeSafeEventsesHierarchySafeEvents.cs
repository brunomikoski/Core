using BrunoMikoski.Core.SafeEvents;
using UnityEngine;

namespace BrunoMikoski.Core.SafeEvents
{
    public class SafeSafeEventsesHierarchySafeEvents : SafeEventsHierarchyNode
    {
        public SafeEventBase eventBase;

        public Object UnityObject
        {
            get
            {
                if (parent == null)
                    return null;
                return parent.UnityObject;
            }
        }
    }
}
