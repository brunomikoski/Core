using System;
using System.Collections;
using System.Threading;
using BrunoMikoski.ScriptableObjectCollections;
using Cysharp.Threading.Tasks;
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

        [SerializeField]
        private int buildIndex;

        public int BuildIndex => buildIndex;

        [SerializeField]
        private bool isNetworkedScene;

        public bool IsNetworkedScene => isNetworkedScene;

        [NonSerialized]
        private Scene cachedScene;

        public Scene Scene
        {
            get
            {
                if (!Application.isPlaying)
                {
                    Debug.LogError("Can't access Scene asset during Editor Time");
                    return default;
                }

                if (!cachedScene.IsValid())
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (scene.name.Equals(sceneName, StringComparison.Ordinal))
                        {
                            cachedScene = scene;
                            break;
                        }
                    }
                }

                return cachedScene;
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            SyncWithSceneAsset();
        }

        public void SyncWithSceneAsset()
        {
            SceneAsset sceneAsset = null;
            string resolvedPath = null;
            string resolvedGuid = null;

            if (!string.IsNullOrEmpty(scenePath))
            {
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null)
                {
                    resolvedPath = scenePath;
                    resolvedGuid = AssetDatabase.AssetPathToGUID(resolvedPath);
                }
            }

            if (sceneAsset == null && !string.IsNullOrEmpty(sceneGuid))
            {
                string pathFromGuid = AssetDatabase.GUIDToAssetPath(sceneGuid);
                if (!string.IsNullOrEmpty(pathFromGuid))
                {
                    sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(pathFromGuid);
                    if (sceneAsset != null)
                    {
                        resolvedPath = pathFromGuid;
                        resolvedGuid = sceneGuid;
                    }
                }
            }

            if (sceneAsset == null && !string.IsNullOrEmpty(sceneName))
            {
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(SceneAsset)} {sceneName}");
                if (guids.Length == 1)
                {
                    resolvedPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(resolvedPath);
                    if (sceneAsset != null)
                    {
                        resolvedGuid = guids[0];
                    }
                }
            }

            bool changed = false;

            if (sceneAsset != null && resolvedPath != null)
            {
                string currentName = sceneAsset.name;
                int currentBuildIndex = GetBuildIndexForPath(resolvedPath);

                if (sceneGuid != resolvedGuid)
                {
                    sceneGuid = resolvedGuid;
                    changed = true;
                }

                if (scenePath != resolvedPath)
                {
                    scenePath = resolvedPath;
                    changed = true;
                }

                if (sceneName != currentName)
                {
                    sceneName = currentName;
                    changed = true;
                }

                if (buildIndex != currentBuildIndex)
                {
                    buildIndex = currentBuildIndex;
                    changed = true;
                }
            }
            else
            {
                // Corrupted or missing: nullate everything
                if (!string.IsNullOrEmpty(sceneGuid))
                {
                    sceneGuid = string.Empty;
                    changed = true;
                }

                if (!string.IsNullOrEmpty(scenePath))
                {
                    scenePath = string.Empty;
                    changed = true;
                }

                if (!string.IsNullOrEmpty(sceneName))
                {
                    sceneName = string.Empty;
                    changed = true;
                }

                if (buildIndex != -1)
                {
                    buildIndex = -1;
                    changed = true;
                }
            }

            if (changed)
                EditorUtility.SetDirty(this);
        }

        private static int GetBuildIndexForPath(string targetPath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path.Equals(targetPath, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
#endif

        public IEnumerator LoadEnumerator(LoadSceneMode loadSceneMode = LoadSceneMode.Additive, bool makeActive = false)
        {
            if (Application.isPlaying)
            {
                bool isSceneLoaded = false;
                SceneManager.LoadSceneAsync(sceneName, loadSceneMode).completed += _ => isSceneLoaded = true;

                while (!isSceneLoaded)
                {
                    yield return null;
                }

                if (makeActive)
                    SceneManager.SetActiveScene(Scene);
            }
#if UNITY_EDITOR
            else
            {
                OpenSceneMode openSceneMode = loadSceneMode == LoadSceneMode.Additive
                    ? OpenSceneMode.Additive
                    : OpenSceneMode.Single;

                SyncWithSceneAsset();
                if (string.IsNullOrEmpty(scenePath))
                    throw new Exception($"Invalid scene data {this.name}");
                EditorSceneManager.OpenScene(scenePath, openSceneMode);
                if (makeActive)
                    SceneManager.SetActiveScene(Scene);
            }
#endif
        }

        public async UniTask Load_Async(LoadSceneMode loadSceneMode = LoadSceneMode.Additive, bool makeActive = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Application.isPlaying)
            {
                await SceneManager.LoadSceneAsync(sceneName, loadSceneMode).ToUniTask(cancellationToken: cancellationToken);
                if (makeActive)
                    SceneManager.SetActiveScene(Scene);
            }
#if UNITY_EDITOR
            else
            {
                OpenSceneMode openSceneMode = loadSceneMode == LoadSceneMode.Additive
                    ? OpenSceneMode.Additive
                    : OpenSceneMode.Single;

                SyncWithSceneAsset();
                if (string.IsNullOrEmpty(scenePath))
                    throw new Exception($"Invalid scene data {this.name}");
                EditorSceneManager.OpenScene(scenePath, openSceneMode);
                if (makeActive)
                    SceneManager.SetActiveScene(Scene);
                await UniTask.CompletedTask;
            }
#endif
        }

        public void Load(LoadSceneMode loadSceneMode = LoadSceneMode.Additive)
        {
            if (Application.isPlaying)
            {
                SceneManager.LoadScene(sceneName, loadSceneMode);
            }
#if UNITY_EDITOR
            else
            {
                OpenSceneMode openSceneMode = loadSceneMode == LoadSceneMode.Additive
                    ? OpenSceneMode.Additive
                    : OpenSceneMode.Single;

                SyncWithSceneAsset();
                if (string.IsNullOrEmpty(scenePath))
                    throw new Exception($"Invalid scene data {this.name}");
                EditorSceneManager.OpenScene(scenePath, openSceneMode);
            }
#endif
        }

        public IEnumerator UnloadEnumerator()
        {
            bool sceneUnloaded = false;
            if (Application.isPlaying)
            {
                SceneManager.UnloadSceneAsync(sceneName).completed += _ => sceneUnloaded = true;

                while (!sceneUnloaded)
                {
                    yield return null;
                }
            }
#if UNITY_EDITOR
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.path.Equals(scenePath, StringComparison.Ordinal))
                    {
                        EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene });
                        EditorSceneManager.CloseScene(scene, true);
                        yield break;
                    }
                }
            }
#endif
        }

        public async UniTask Unload_Async(CancellationToken cancellationToken = default)
        {
            if (!IsLoaded())
                return;


            cancellationToken.ThrowIfCancellationRequested();
            if (Application.isPlaying)
            {
                await SceneManager.UnloadSceneAsync(Scene).ToUniTask(cancellationToken: cancellationToken);
            }
#if UNITY_EDITOR
            else
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.path.Equals(scenePath, StringComparison.Ordinal))
                    {
                        EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene });
                        EditorSceneManager.CloseScene(scene, true);
                        break;
                    }
                }

                await UniTask.CompletedTask;
            }
#endif
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
                        EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene });
                        EditorSceneManager.CloseScene(scene, true);
                        return;
                    }
                }
            }
#endif
        }

        public bool IsLoaded()
        {
            return Scene.isLoaded;
        }

        public bool IsActive()
        {
            return SceneManager.GetActiveScene() == Scene;
        }

#if PHOTON_AVAILABLE
        public SceneRef AsSceneRef()
        {
            return SceneRef.FromIndex(BuildIndex);
        }
#endif
    }
}
