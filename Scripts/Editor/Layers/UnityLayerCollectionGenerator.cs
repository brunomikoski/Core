using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.Core.Layers;
using BrunoMikoski.ScriptableObjectCollections;
using JetBrains.Annotations;
using UnityEngine;

namespace BrunoMikoski.Core.Editor.Layers
{
    public class UnityLayerTemplate : ItemTemplate<SingleUnityLayer>
    {
        public int layerIndex;
    }
    
    [UsedImplicitly]
    public sealed class UnityLayerCollectionGenerator : IScriptableObjectCollectionGenerator<UnityLayerCollection, UnityLayerTemplate>
    {
        public bool ShouldRemoveNonGeneratedItems => false;

        public void GetItemTemplates(List<UnityLayerTemplate> templates, UnityLayerCollection collection)
        {
            string[] layers = Enumerable.Range(0, 31).Select(LayerMask.LayerToName).Where(l => !string.IsNullOrEmpty(l)).ToArray();

            for (int i = 0; i < layers.Length; i++)
            {
                templates.Add(new UnityLayerTemplate { name = layers[i], layerIndex = i});
            }
        }
    }
}
