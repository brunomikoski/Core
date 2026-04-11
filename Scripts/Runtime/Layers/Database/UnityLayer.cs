using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core.Layers
{
    public abstract partial class UnityLayer : ScriptableObjectCollectionItem
    {
        public static implicit operator LayerMask(UnityLayer unityLayer)
        {
            return unityLayer.ToLayerMask();
        }

        public static implicit operator int(UnityLayer unityLayer)
        {
            return unityLayer.Index;
        }
    }
}
