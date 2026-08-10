using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Layers
{
    public class SingleUnityLayer : UnityLayer
    {
        [SerializeField]
        private int layerIndex;
        public int LayerIndex => layerIndex;
    }
}