using System;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.ScenesReferences
{
    [InitializeOnLoad]
    public static class UnitySceneBuildListWatcher
    {
        private static bool syncQueued;

        static UnitySceneBuildListWatcher()
        {
            EditorBuildSettings.sceneListChanged -= OnSceneListChanged;
            EditorBuildSettings.sceneListChanged += OnSceneListChanged;
        }

        private static void OnSceneListChanged()
        {
            SyncAllScenes();
        }

        internal static void QueueSyncAllScenes()
        {
            if (syncQueued)
                return;

            syncQueued = true;
            EditorApplication.delayCall += () =>
            {
                syncQueued = false;
                SyncAllScenes();
            };
        }

        private static void SyncAllScenes()
        {
            if (!CollectionsRegistry.Instance.TryGetCollectionOfType(out UnityScenesCollection collection))
                return;

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (UnityScene scene in collection)
                    scene.SyncWithSceneAsset();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }
        }
    }

    internal class UnitySceneMoveWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < movedAssets.Length; i++)
            {
                if (!movedAssets[i].EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    continue;

                UnitySceneBuildListWatcher.QueueSyncAllScenes();
                return;
            }
        }
    }
}
