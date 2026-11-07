using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public readonly struct ReferencingProperty
    {
        public readonly Component Owner;
        public readonly string Path;

        public ReferencingProperty(Component owner, string path)
        {
            Owner = owner;
            Path = path;
        }
    }
}
