#if PHOTON_AVAILABLE
using Fusion;
#endif
using UnityEngine.SceneManagement;

namespace BrunoMikoski
{
    public static class SceneExtensions
    {
#if PHOTON_AVAILABLE
        public static SceneRef AsSceneRef(this Scene unityScene)
        {
            return SceneRef.FromIndex(unityScene.buildIndex);
        }
#endif
    }
}