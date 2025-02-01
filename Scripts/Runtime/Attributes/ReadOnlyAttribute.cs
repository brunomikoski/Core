using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    public class ReadOnlyAttribute : ConditionalAttribute
    {
        public ReadOnlyAttribute()
        {
        }

        public ReadOnlyAttribute(string dependingOnField, params object[] whenValues)
            : base(dependingOnField, whenValues)
        {
        }
    }
}