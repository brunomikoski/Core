using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public readonly struct TriggerContext
    {
        private readonly Component instigator;
        private readonly Vector3 point;
        private readonly Vector3 normal;
        private readonly float strength;
        private readonly bool hasPoint;
        private readonly bool hasStrength;

        public TriggerContext(Component instigator, Vector3 point, Vector3 normal, float strength)
        {
            this.instigator = instigator;
            this.point = point;
            this.normal = normal;
            this.strength = strength;
            hasPoint = true;
            hasStrength = true;
        }

        public bool TryGetInstigator(out Component value)
        {
            value = instigator;
            return instigator != null;
        }

        public bool TryGetPoint(out Vector3 value, out Vector3 surfaceNormal)
        {
            value = point;
            surfaceNormal = normal;
            return hasPoint;
        }

        public bool TryGetStrength(out float value)
        {
            value = strength;
            return hasStrength;
        }
    }
}
