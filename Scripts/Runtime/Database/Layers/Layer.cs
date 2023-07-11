using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core.Layers
{
    public partial class Layer : ScriptableObjectCollectionItem
    {
        [SerializeField]
        private int layerIndex;
        public int LayerIndex => layerIndex;
    }
}
