using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static class EditorBootstrapUtility
    {
        private const string EDITOR_BOOTSTRAP_OPEN_SCENES_KEY = "EditorBootstrap_OpenScenes";
        private const string EDITOR_BOOT_SKIP_KEY = "EDITOR_SCENE_SKIP";
        private const string EDITOR_SCENE_SETUP_SNAPSHOT_KEY = "EDITOR_SCENE_SETUP_SNAPSHOT";

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (!EditorBootstrapSettings.Instance.LoadOpenScenesAfterBootstrap
                || !EditorBootstrapSettings.Instance.HasBootstrapSetup()
                || IsCurrentlyAtUntitledScene()
                || EditorSceneManager.playModeStartScene != null
                || IsPlayingFromFirstSceneOnBootSettings()
                || !IsPlayingFromAKnownScene())
            {
                SessionState.SetBool(EDITOR_BOOT_SKIP_KEY, false);
                SessionState.EraseString(EDITOR_BOOTSTRAP_OPEN_SCENES_KEY);
                return;
            }

            if (stateChange == PlayModeStateChange.ExitingEditMode)
            {
                SceneSetup sceneSetupSnapshot = GetCurrentSceneSetupSnapshot();

                for (int i = 0; i < EditorBootstrapSettings.Instance.BootStrapScenesGuid.Count; i++)
                {
                    string boostrapSceneGUID = EditorBootstrapSettings.Instance.BootStrapScenesGuid[i];
                    string scenePath = AssetDatabase.GUIDToAssetPath(boostrapSceneGUID);
                    SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (sceneAsset == null)
                        continue;

                    if (IsSceneAlreadyOpen(sceneAsset.name))
                        continue;

                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    SceneManager.SetActiveScene(GetSceneByName(sceneAsset.name));
                    sceneSetupSnapshot.LoadedBootstrapSceneGuids.Add(boostrapSceneGUID);
                }

                if (sceneSetupSnapshot.LoadedBootstrapSceneGuids.Count == 0)
                {
                    SessionState.SetBool(EDITOR_BOOT_SKIP_KEY, false);
                    SessionState.EraseString(EDITOR_SCENE_SETUP_SNAPSHOT_KEY);
                    return;
                }

                SceneAsset lastAutomaticallyLoadedScene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(sceneSetupSnapshot.LoadedBootstrapSceneGuids[^1]));

                foreach (string originalLoadedSceneGUID in sceneSetupSnapshot.OriginalLoadedScenesGuids)
                {
                    SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(originalLoadedSceneGUID));
                    EditorSceneManager.MoveSceneAfter(GetSceneByName(sceneAsset.name), GetSceneByName(lastAutomaticallyLoadedScene.name));
                }

                SessionState.SetBool(EDITOR_BOOT_SKIP_KEY, true);
                SessionState.SetString(EDITOR_SCENE_SETUP_SNAPSHOT_KEY, JsonUtility.ToJson(sceneSetupSnapshot));
            }
            else if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                string storedSessionJson = SessionState.GetString(EDITOR_SCENE_SETUP_SNAPSHOT_KEY, "");
                if (string.IsNullOrEmpty(storedSessionJson))
                    return;

                SceneSetup sceneSetupSnapshot = JsonUtility.FromJson<SceneSetup>(storedSessionJson);
                SceneManager.SetActiveScene(GetSceneByName(sceneSetupSnapshot.ActiveSceneName));
                EditorBootstrap.OnEditorBootstrapCompleteEvent?.Invoke();
            }
            else if (stateChange == PlayModeStateChange.EnteredEditMode)
            {
                string storedSessionJson = SessionState.GetString(EDITOR_SCENE_SETUP_SNAPSHOT_KEY, "");
                if (string.IsNullOrEmpty(storedSessionJson))
                    return;

                SceneSetup sceneSetupSnapshot = JsonUtility.FromJson<SceneSetup>(storedSessionJson);

                for (int i = 0; i < sceneSetupSnapshot.LoadedBootstrapSceneGuids.Count; i++)
                {
                    string loadedBootstrapSceneGuid = sceneSetupSnapshot.LoadedBootstrapSceneGuids[i];
                    string scenePath = AssetDatabase.GUIDToAssetPath(loadedBootstrapSceneGuid);
                    SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                    if (sceneAsset == null)
                        continue;
                    EditorSceneManager.CloseScene(GetSceneByName(sceneAsset.name), true);
                }

                SceneManager.SetActiveScene(GetSceneByName(sceneSetupSnapshot.ActiveSceneName));
                SessionState.EraseString(EDITOR_SCENE_SETUP_SNAPSHOT_KEY);
            }
        }

        private static bool IsPlayingFromAKnownScene()
        {
            string activeScenePath = SceneManager.GetActiveScene().path;

            if (string.IsNullOrEmpty(activeScenePath))
                return false;

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (string.Equals(scene.path, activeScenePath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static Scene GetSceneByName(string targetName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name.Equals(targetName, StringComparison.Ordinal))
                    return scene;
            }

            return default;
        }

        [Serializable]
        private class SceneSetup
        {
            [SerializeField]
            internal string ActiveSceneName = string.Empty;

            [SerializeField]
            internal List<string> OriginalLoadedScenesGuids = new();

            [SerializeField]
            internal List<string> LoadedBootstrapSceneGuids = new List<string>();
        }

        private static SceneSetup GetCurrentSceneSetupSnapshot()
        {
            SceneSetup sceneSetup = new SceneSetup();

            for (int i = 0; i < EditorSceneManager.loadedRootSceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    if (scene == SceneManager.GetActiveScene())
                        sceneSetup.ActiveSceneName = scene.name;

                    sceneSetup.OriginalLoadedScenesGuids.Add(AssetDatabase.AssetPathToGUID(scene.path));
                }
            }

            return sceneSetup;
        }

        private static bool IsPlayingFromFirstSceneOnBootSettings()
        {
            Scene currentActiveScene = SceneManager.GetActiveScene();
            if (string.Equals(currentActiveScene.path, EditorBuildSettings.scenes[0].path,
                    StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }


        private static bool IsCurrentlyAtUntitledScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string sceneName = SceneManager.GetSceneAt(i).name;
                if (string.IsNullOrEmpty(sceneName) || sceneName.IndexOf("Untitled", StringComparison.Ordinal) > -1)
                    return true;
            }

            return false;
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
