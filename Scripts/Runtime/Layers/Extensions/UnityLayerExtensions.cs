using BrunoMikoski.Core.Layers;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public static class UnityLayerExtensions
    {
        public static LayerMask ToLayerMask(this UnityLayer unityLayer)
        {
            if (unityLayer is SingleUnityLayer singleUnityLayer)
                return 1 << singleUnityLayer.LayerIndex;
            if (unityLayer is GroupUnityLayer unityLayerGroup)
                return unityLayerGroup.Layers.ToLayerMask();
            
            return 0;
        }

        public static LayerMask ToLayerMask(this CollectionItemPicker<SingleUnityLayer> layers)
        {
            LayerMask layerMask = new()
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