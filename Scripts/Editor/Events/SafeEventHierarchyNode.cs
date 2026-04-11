namespace BrunoMikoski.Core.Events
{
    public class SafeEventHierarchyNode
    {
        public SafeEventHierarchyGroup parent;
        public string name;

        public override string ToString()
        {
            if (parent != null)
                return parent.ToString() + "/" + name;
            return name;
        }
    }
}
