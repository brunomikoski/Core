using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core.Layers
{
    public abstract class UnityLayer : ScriptableObjectCollectionItem
    {
        public static implicit operator LayerMask(UnityLayer unityLayer)
        {
            return unityLayer.ToLayerMask();
        }
    }
}
