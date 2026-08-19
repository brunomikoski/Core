#if UNITY_LOCALIZATION_AVAILABLE
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace BrunoMikoski.Core.Utils.Localization
{
    [InitializeOnLoad]
    internal static class LocalizationPreviewLocaleAutoSelector
    {
        static LocalizationPreviewLocaleAutoSelector()
        {
            if (Application.isBatchMode)
                return;

            EditorApplication.delayCall += SelectProjectLocaleIfNoneActive;
        }

        private static void SelectProjectLocaleIfNoneActive()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!LocalizationSettings.HasSettings || LocalizationSettings.SelectedLocale != null)
                return;

            Locale projectLocale = LocalizationSettings.ProjectLocale;
            if (projectLocale == null)
            {
                Debug.LogWarning("Localization Settings has no valid Project Locale; edit-mode localization preview stays disabled.");
                return;
            }

            LocalizationSettings.SelectedLocale = projectLocale;
        }
    }
}
#endif
