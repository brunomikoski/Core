using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.Core.SaveCallback
{
    internal class SaveCallbackAssetProcessor : AssetModificationProcessor
    {
        private const string SaveBlockedTitle = "Save Blocked";

        private static string[] OnWillSaveAssets(string[] paths)
        {
            List<string> pathsToSave = new List<string>(paths);

            foreach (string path in paths)
            {
                if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    if (!ValidateSceneSave(path, out string message))
                    {
                        EditorUtility.DisplayDialog(SaveBlockedTitle, message, "OK");
                        pathsToSave.Remove(path);
                    }
                }
                else if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    if (!ValidatePrefabSave(path, out string message))
                    {
                        EditorUtility.DisplayDialog(SaveBlockedTitle, message, "OK");
                        pathsToSave.Remove(path);
                    }
                }
            }

            return pathsToSave.ToArray();
        }

        private static bool ValidateSceneSave(string scenePath, out string failureMessage)
        {
            failureMessage = null;
            Scene scene = SceneManager.GetSceneByPath(scenePath);

            if (!scene.IsValid() || !scene.isLoaded)
                return true;

            IOnSaveScene[] validators = GetComponentsInScene<IOnSaveScene>(scene);

            foreach (IOnSaveScene validator in validators)
            {
                SaveValidationResult result = validator.CanSaveScene(scene);
                if (!result.CanSave)
                {
                    failureMessage = result.Message ?? "A component blocked the save.";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidatePrefabSave(string prefabPath, out string failureMessage)
        {
            failureMessage = null;

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null || !prefabStage.assetPath.Equals(prefabPath, StringComparison.Ordinal))
                return true;

            Scene prefabScene = prefabStage.scene;
            IOnSavePrefab[] validators = GetComponentsInScene<IOnSavePrefab>(prefabScene);

            foreach (IOnSavePrefab validator in validators)
            {
                SaveValidationResult result = validator.CanSavePrefab(prefabPath);
                if (!result.CanSave)
                {
                    failureMessage = result.Message ?? "A component blocked the save.";
                    return false;
                }
            }

            return true;
        }

        private static T[] GetComponentsInScene<T>(Scene scene) where T : class
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return Array.Empty<T>();

            GameObject[] rootObjects = scene.GetRootGameObjects();
            var results = new List<T>();

            foreach (GameObject root in rootObjects)
            {
                foreach (Component c in root.GetComponentsInChildren<Component>(true))
                {
                    if (c is T t)
                        results.Add(t);
                }
            }

            return results.ToArray();
        }
    }
}
