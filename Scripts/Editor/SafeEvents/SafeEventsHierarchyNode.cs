namespace BrunoMikoski.Core.SafeEvents
{
    public class SafeEventsHierarchyNode
    {
        public SafeSafeEventsesHierarchyGroup parent;
        public string name;

        public override string ToString()
        {
            if (parent != null)
                return parent.ToString() + "/" + name;
            return name;
        }
    }
}
