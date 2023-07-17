using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            if (!EditorBootstrapSettings.Instance.LoadOpenScenesAfterBootstrap || !EditorBootstrapSettings.Instance.HasBootstrapSetup())
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SceneAsset bootstrapScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBootstrapSettings.Instance.BootstrapScenePath);

            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                if (EditorBuildSettings.scenes.Length > 0 &&
                    IsSceneAlreadyOpen(Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[0].path)))
                {
                    EditorSceneManager.playModeStartScene = null;
                    return;
                }

                EditorSceneManager.playModeStartScene = bootstrapScene == null ? null : bootstrapScene;
                
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

                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneLoaded += OnSceneLoaded;

                string[] scenesToOpen = scenes.Split(',', StringSplitOptions.RemoveEmptyEntries);

                for (int i = scenesToOpen.Length - 1; i >= 0; i--)
                {
                    string sceneToOpen = scenesToOpen[i];

                    if (IsSceneAlreadyOpen(sceneToOpen))
                        continue;

                    SceneManager.LoadScene(sceneToOpen, LoadSceneMode.Additive);
                }
            }
        }

        private static bool IsSceneAlreadyOpen(string targetSceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name.IndexOf(targetSceneName, StringComparison.Ordinal) > -1)
                    return true;
            }

            return false;
        }

        private static void OnSceneLoaded(Scene targetScene, LoadSceneMode sceneMode)
        {
            if (!Application.isPlaying)
                return;

            string scenes = SessionState.GetString(EDITOR_BOOTSTRAP_OPEN_SCENES_KEY, "");

            string expectedActiveSceneName = scenes.Split(',', StringSplitOptions.RemoveEmptyEntries)[0];
            if (!string.Equals(expectedActiveSceneName, targetScene.name, StringComparison.Ordinal))
                return;

            SceneManager.SetActiveScene(targetScene);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}