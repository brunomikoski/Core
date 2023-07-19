using BrunoMikoski.Core.Layers;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public static class LayerIDExtensions
    {
        public static LayerMask ToLayerMask(this UnityLayer unityLayer)
        {
            return 1 << unityLayer.LayerIndex;
        }

        public static LayerMask ToLayerMask(this CollectionItemPicker<UnityLayer> layers)
        {
            LayerMask layerMask = new LayerMask
            {
                value = 0
            };
            
            for (int i = 0; i < layers.Count; i++)
            {
                layerMask.value |= 1 << layers[i].LayerIndex;
            }

            return layerMask;
        }
    }
}