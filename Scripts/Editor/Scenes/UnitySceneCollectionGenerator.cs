using System;
using System.Collections.Generic;
using System.IO;
using BrunoMikoski.Core.DevelopmentTools.ScenesReferences;
using BrunoMikoski.ScriptableObjectCollections;
using JetBrains.Annotations;
using UnityEditor;

namespace BrunoMikoski.Core.ScenesReferences
{
    public class UnitySceneTemplate : ItemTemplate
    {
        public string sceneGuid;
        public string scenePath;
        public string sceneName;
        public int buildIndex;
    }
    
    [UsedImplicitly]
    public sealed class UnitySceneCollectionGenerator : IScriptableObjectCollectionGenerator<UnityScenesCollection, UnitySceneTemplate>
    {
        public bool ShouldRemoveNonGeneratedItems => false;

        public void GetItemTemplates(List<UnitySceneTemplate> templates, UnityScenesCollection collection)
        {
            
            string[] possibleAssets = AssetDatabase.FindAssets($"t:{nameof(SceneAsset)}");

            for (int i = 0; i < possibleAssets.Length; i++)
            {
                string possibleAssetGUID = possibleAssets[i];
                string scenePath = AssetDatabase.GUIDToAssetPath(possibleAssetGUID);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);

                if (IsIgnored(collection, scenePath))
                    continue;
                
                TryGetUnityBuildSettingsScene(scenePath, out EditorBuildSettingsScene _, out int buildIndex);

                templates.Add(new UnitySceneTemplate()
                    { name = sceneName, sceneGuid = possibleAssetGUID, scenePath = scenePath, sceneName = sceneName, buildIndex = buildIndex });
            }
        }

        
        private bool IsIgnored(ScriptableObjectCollection collection, string scenePath)
        {
            SerializedObject collectionSerializedObject = new SerializedObject(collection);
            SerializedProperty ignoredPaths = collectionSerializedObject.FindProperty("ignoredScenePaths");

            for (int i = 0; i < ignoredPaths.arraySize; i++)
            {
                SerializedProperty ignoredPath = ignoredPaths.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(ignoredPath.stringValue))
                    continue;

                if (scenePath.IndexOf(ignoredPath.stringValue, StringComparison.Ordinal) > -1)
                    return true;
            }

            return false;
        }
        
        private bool TryGetUnityBuildSettingsScene(string scenePath, out EditorBuildSettingsScene editorBuildSettingsScene, out int index)
        {
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                EditorBuildSettingsScene buildSettingsScene = EditorBuildSettings.scenes[i];
                if (buildSettingsScene.path.Equals(scenePath, StringComparison.Ordinal))
                {
                    editorBuildSettingsScene = buildSettingsScene;
                    index = i;
                    return true;
                }
            }

            index = -1;
            editorBuildSettingsScene = null;
            return false;
        }
    }
}