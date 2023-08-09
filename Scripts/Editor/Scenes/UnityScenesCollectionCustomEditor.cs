using System;
using System.IO;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.ScenesReferences
{
    [CustomEditor(typeof(UnityScenesCollection), true)]
    public class UnityScenesCollectionCustomEditor : CollectionCustomEditor
    {
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Sync"))
            {
                SyncScenes();
            }
        }

        private void SyncScenes()
        {
            string[] possibleAssets = AssetDatabase.FindAssets($"t:{nameof(SceneAsset)}");

            for (int i = 0; i < possibleAssets.Length; i++)
            {
                string possibleAssetGUID = possibleAssets[i];
                string scenePath = AssetDatabase.GUIDToAssetPath(possibleAssetGUID);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                
                if(IsIgnored(scenePath))
                    continue;

                TryGetUnityBuildSettingsScene(scenePath, out EditorBuildSettingsScene _, out int buildIndex);
                
                UnityScene targetUnityScene = GetOrAddSceneObject(scenePath, possibleAssetGUID);
                
                if (!string.Equals(targetUnityScene.name, sceneName, StringComparison.Ordinal))
                {
                    string newPath = scenePath.Replace(Path.GetFileName(scenePath), sceneName);
                    AssetDatabase.RenameAsset(scenePath, newPath);
                    scenePath = newPath;
                    AssetDatabase.SaveAssetIfDirty(targetUnityScene);
                }
                
                SerializedObject unitySceneSO = new SerializedObject(targetUnityScene);
                unitySceneSO.FindProperty("sceneGuid").stringValue = possibleAssetGUID;
                unitySceneSO.FindProperty("scenePath").stringValue = scenePath;
                unitySceneSO.FindProperty("sceneName").stringValue = sceneName;
                unitySceneSO.FindProperty("buildIndex").intValue = buildIndex;
                unitySceneSO.ApplyModifiedProperties();
            }
            OnEnable();
            Repaint();
        }

        private bool IsIgnored(string scenePath)
        {
            SerializedProperty ignoredPaths = serializedObject.FindProperty("ignoredScenePaths");

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

        private UnityScene GetOrAddSceneObject(string scenePath, string sceneGUID)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                UnityScene unityScene = (UnityScene) collection[i];

                if (unityScene.ScenePath.Equals(scenePath, StringComparison.Ordinal))
                    return unityScene;

                if (unityScene.SceneGuid.Equals(sceneGUID, StringComparison.Ordinal))
                    return unityScene;
            }

            return (UnityScene) collection.AddNew(collection.GetItemType(), Path.GetFileNameWithoutExtension(scenePath));
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
