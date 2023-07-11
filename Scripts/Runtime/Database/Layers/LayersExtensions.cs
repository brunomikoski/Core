using BrunoMikoski.ScriptableObjectCollections.Picker;
using UnityEngine;

namespace BrunoMikoski.Core.Layers
{
    public static class LayersExtensions
    {
        public static LayerMask ToLayerMask(this Layer layer)
        {
            return 1 << layer.LayerIndex;
        }

        public static LayerMask ToLayerMask(this CollectionItemPicker<Layer> layers)
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