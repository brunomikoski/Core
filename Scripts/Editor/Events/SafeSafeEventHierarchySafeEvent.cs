using BrunoMikoski.Core.Events;
using UnityEngine;

namespace BrunoMikoski.Core.Events
{
    public class SafeSafeEventHierarchySafeEvent : SafeEventHierarchyNode
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
