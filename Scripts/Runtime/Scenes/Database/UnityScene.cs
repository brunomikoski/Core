using System;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BrunoMikoski.Core.ScenesReferences
{
    public partial class UnityScene : ScriptableObjectCollectionItem
    {
        [SerializeField, HideInInspector]
        private string sceneGuid;
        public string SceneGuid => sceneGuid;

        [SerializeField, HideInInspector]
        private string sceneName;
        public string SceneName => sceneName;

        [SerializeField, HideInInspector]
        private string scenePath;
        public string ScenePath => scenePath;

        [SerializeField, HideInInspector]
        private int buildIndex;
        public int BuildIndex => buildIndex;

        public void Load(LoadSceneMode loadSceneMode = LoadSceneMode.Additive)
        {
            if (Application.isPlaying)
            {
                SceneManager.LoadScene(sceneName, loadSceneMode);
            }
#if UNITY_EDITOR
            else
            {
                OpenSceneMode openSceneMode = loadSceneMode == LoadSceneMode.Additive ? OpenSceneMode.Additive
                    : OpenSceneMode.Single;

                ValidateSceneAsset();
                
                EditorSceneManager.OpenScene(scenePath, openSceneMode);
            }
#endif
        }

        private void ValidateSceneAsset()
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset != null)
                return;
            

            string targetScenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(targetScenePath);
            if (sceneAsset != null)
            {
                scenePath = targetScenePath;
                EditorUtility.SetDirty(this);
                return;
            }
                
            string[] possibleSceneGUIDs = AssetDatabase.FindAssets($"t:{nameof(SceneAsset)} {sceneName}");
            string targetSceneGUID = "";
            if (possibleSceneGUIDs.Length == 1)
            {
                targetSceneGUID = possibleSceneGUIDs[0];
                targetScenePath = AssetDatabase.GUIDToAssetPath(targetSceneGUID);
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(targetScenePath);
            }

            if (sceneAsset != null)
            {
                sceneName = sceneAsset.name;
                scenePath = targetScenePath;
                sceneGuid = targetSceneGUID;
                EditorUtility.SetDirty(this);
                return;
            }
            throw new Exception($"Invalid scene data {this.name}");
        }

        public void Unload()
        {
            if (Application.isPlaying)
            {
                SceneManager.UnloadSceneAsync(sceneName);
            }
#if UNITY_EDITOR
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.path.Equals(scenePath, StringComparison.Ordinal))
                    {
                        EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] {scene});
                        EditorSceneManager.CloseScene(scene, true);
                        return;
                    }
                }
            }
#endif
        }

        public bool IsLoaded()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name.Equals(sceneName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
