#if UNITY_LOCALIZATION_AVAILABLE
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace BrunoMikoski.Core.Utils.Localization
{
    internal sealed class LocalizationKeysAutoGenerator : AssetPostprocessor
    {
        private static bool _regenerationQueued;

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Application.isBatchMode)
                return;

            if (!LocalizationKeysGeneratorSettings.Instance.IsConfigured())
                return;

            if (!ContainsSharedTableData(importedAssets) && !ContainsSharedTableData(movedAssets))
                return;

            RequestRegeneration();
        }

        private static bool ContainsSharedTableData(string[] paths)
        {
            foreach (string path in paths)
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    continue;

                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (typeof(SharedTableData).IsAssignableFrom(assetType))
                    return true;
            }

            return false;
        }

        private static void RequestRegeneration()
        {
            if (_regenerationQueued)
                return;

            _regenerationQueued = true;
            EditorApplication.delayCall += RunQueuedRegeneration;
        }

        private static void RunQueuedRegeneration()
        {
            _regenerationQueued = false;
            LocalizationKeysGenerator.Generate();
        }
    }
}
#endif
