using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Playground.ScenesReferences
{
    public partial class SceneID : ScriptableObjectCollectionItem
    {
        [SerializeField]
        private string sceneGuid;
        public string SceneGuid => sceneGuid;

        [SerializeField]
        private string sceneName;
        public string SceneName => sceneName;

        [SerializeField]
        private string scenePath;
        public string ScenePath => scenePath;
    }
}
