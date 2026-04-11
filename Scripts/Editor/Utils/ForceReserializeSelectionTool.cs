using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Editor
{
    public static class ForceReserializeSelectionTool
    {
        [MenuItem("Assets/Force Reserialize Selected", false, 2600)]
        private static void ForceReserializeSelected()
        {
            Object[] selectedObjects = Selection.GetFiltered<Object>(SelectionMode.Assets);
            if (selectedObjects.Length == 0)
                return;

            List<string> assetPaths = new List<string>();
            foreach (Object selectedObject in selectedObjects)
            {
                string selectedPath = AssetDatabase.GetAssetPath(selectedObject);
                if (string.IsNullOrWhiteSpace(selectedPath))
                {
                    continue;
                }

                if (AssetDatabase.IsValidFolder(selectedPath))
                {
                    string[] childAssetGuids = AssetDatabase.FindAssets(string.Empty, new[] { selectedPath });
                    foreach (string guid in childAssetGuids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (string.IsNullOrWhiteSpace(assetPath))
                        {
                            continue;
                        }

                        assetPaths.Add(assetPath);
                    }

                    continue;
                }

                assetPaths.Add(selectedPath);
            }

            if (assetPaths.Count == 0)
                return;

            HashSet<string> uniqueAssetPaths = new HashSet<string>(assetPaths);
            List<string> sortedUniqueAssetPaths = new List<string>(uniqueAssetPaths);
            sortedUniqueAssetPaths.Sort();

            AssetDatabase.ForceReserializeAssets(
                sortedUniqueAssetPaths,
                ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Force-reserialized {sortedUniqueAssetPaths.Count} assets from current selection.");
        }

        [MenuItem("Assets/Force Reserialize Selected", true)]
        private static bool ValidateForceReserializeSelected()
        {
            return Selection.GetFiltered<Object>(SelectionMode.Assets).Length > 0;
        }
    }
}
