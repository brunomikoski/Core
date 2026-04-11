using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core.ScenesReferences
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Create UnityScenesCollection", fileName = "UnityScenesCollection", order = 0)]
    public class UnityScenesCollection : ScriptableObjectCollection<UnityScene>
    {
        [SerializeField]
        private string[] ignoredScenePaths;

        public bool TryGetSceneByBuildIndex(int targetSceneBuildIndex, out UnityScene unityScene)
        {
            foreach (UnityScene scene in Values)
            {
                if (scene.BuildIndex == targetSceneBuildIndex)
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
