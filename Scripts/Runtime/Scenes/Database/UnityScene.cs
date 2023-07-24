using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Playground.ScenesReferences
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
    }
}
