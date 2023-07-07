using System;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.Core
{
    public static class EditorBootstrap
    {
        private const string EDITOR_BOOTSTRAP_OPEN_SCENES_KEY = "EditorBootstrap_OpenScenes";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if(!EditorBootstrapSettings.Instance.LoadOpenScenesAfterBootstrap)
                return;

            SceneAsset bootstrapScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBootstrapSettings.Instance.BootstrapScenePath);
            
            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                string storageStringOpenScenes = "";
                Scene activeScene = SceneManager.GetActiveScene();
                if (!activeScene.name.Equals(bootstrapScene.name, StringComparison.Ordinal))
                    storageStringOpenScenes = $"{activeScene.name}";
                
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.buildIndex == activeScene.buildIndex)
                        continue;

                    if (scene.name.Equals(bootstrapScene.name, StringComparison.Ordinal))
                        continue;
                    
                    storageStringOpenScenes += $", {scene.name}";
                }

                SessionState.SetString(EDITOR_BOOTSTRAP_OPEN_SCENES_KEY, storageStringOpenScenes);
            }
            else if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                string scenes = SessionState.GetString(EDITOR_BOOTSTRAP_OPEN_SCENES_KEY, "");
                if (string.IsNullOrEmpty(scenes))
                    return;
                
                SceneManager.sceneLoaded += OnSceneLoaded;
    
                string[] scenesToOpen = scenes.Split(',', StringSplitOptions.RemoveEmptyEntries);

                for (int i = scenesToOpen.Length - 1; i >= 0; i--)
                {
                    string sceneToOpen = scenesToOpen[i];

                    SceneManager.LoadScene(sceneToOpen, LoadSceneMode.Additive);
                }
            }
        }

        private static void OnSceneLoaded(Scene targetScene, LoadSceneMode sceneMode)
        {
            string scenes = SessionState.GetString(EDITOR_BOOTSTRAP_OPEN_SCENES_KEY, "");
            
            string expectedActiveSceneName = scenes.Split(',', StringSplitOptions.RemoveEmptyEntries)[0];
            if (!string.Equals(expectedActiveSceneName, targetScene.name, StringComparison.Ordinal)) 
                return;
            
            SceneManager.SetActiveScene(targetScene);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}