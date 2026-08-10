#if UNITY_LOCALIZATION_AVAILABLE
using UnityEngine.Localization;
#endif

namespace BrunoMikoski.Core.Utils.Localization
{
    public interface ILocalizedDisplayValue
    {
#if UNITY_LOCALIZATION_AVAILABLE
        public LocalizedString DisplayValue { get; }
#endif
        
    }
}
