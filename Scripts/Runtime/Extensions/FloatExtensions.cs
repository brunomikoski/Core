using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static partial class FloatExtensions
    {
        public static bool Approximately(this float left, float right)
        {
            return Mathf.Approximately(left, right);
        }
    }
}