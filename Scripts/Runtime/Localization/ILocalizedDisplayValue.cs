using UnityEngine;

namespace Brunomikoski.Core.Localization
{
#if LOCALIZATION_ENABLED
    public interface ILocalizedDisplayValue
    {
        public LocalizedString DisplayValue { get; }
    }
#endif

}
