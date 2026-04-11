using BrunoMikoski.Core.ScenesReferences;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine.SceneManagement;

namespace BrunoMikoski
{
    public static class UnitySceneExtensions
    {
        public static UnityScene AsUnityScene(this Scene unityScene)
        {
            if (CollectionsRegistry.Instance.TryGetCollectionOfType(out UnityScenesCollection unitySceneCollection))
            {
                unitySceneCollection.TryGetSceneByBuildIndex(unityScene.buildIndex, out UnityScene result);
                return result;
            }

            return null;
        }
        
        public static bool TryGetAsUnityScene(this Scene scene, out  UnityScene resultUnityScene)
        {
            if (CollectionsRegistry.Instance.TryGetCollectionOfType(out UnityScenesCollection unitySceneCollection))
            {
                return unitySceneCollection.TryGetSceneByBuildIndex(scene.buildIndex, out resultUnityScene);
            }

            resultUnityScene = null;
            return false;
        }
    }
}
