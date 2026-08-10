using UnityEngine;

namespace BrunoMikoski.Core.Utils
{
    public static partial class ColorHelpers
    {
        private const int UINT24_MAX_VALUE = 16777216;

        public static Color GetUniqueColor(Object @object)
        {
            ulong entityId = EntityId.ToULong(@object.GetEntityId());
            return GetUniqueColor((int)entityId);
        }

        public static Color GetUniqueColor(string name)
        {
            return GetUniqueColor(name.GetHashCode());
        }

        public static Color GetUniqueColor(int seed)
        {
            Random.InitState(seed);
            int random = Random.Range(0, UINT24_MAX_VALUE);

            int r = (random >> 0) % 256;
            int g = (random >> 8) % 256;
            int b = (random >> 16) % 256;

            return new Color(r / 255f, g / 255f, b / 255f);
        }
    }
}
