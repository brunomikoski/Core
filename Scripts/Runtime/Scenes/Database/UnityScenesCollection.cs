using System;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrunoMikoski.Core.DevelopmentTools.ScenesReferences
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Create UnityScenesCollection", fileName = "UnityScenesCollection", order = 0)]
    public class UnityScenesCollection : ScriptableObjectCollection<UnityScene>
    {
        [SerializeField]
        private string[] ignoredScenePaths;

        public bool TryGetUnitySceneByScene(Scene targetScene, out UnityScene unityScene)
        {
            foreach (UnityScene scene in Values)
            {
                if (scene.ScenePath.Equals(targetScene.path, StringComparison.Ordinal))
                {
                    unityScene = scene;
                    return true;
                }
            }

            foreach (UnityScene scene in Values)
            {
                if (scene.SceneName.Equals(targetScene.name, StringComparison.Ordinal))
                {
                    unityScene = scene;
                    return true;
                }
            }

            unityScene = null;
            return false;
        }
    }
}
