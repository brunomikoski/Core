using BrunoMikoski.ScriptableObjectCollections.Picker;
using UnityEngine;

namespace BrunoMikoski.Core.Layers
{
    public partial class GroupUnityLayer : UnityLayer
    {
        [SerializeField]
        private CollectionItemPicker<SingleUnityLayer> layers = new();
        public CollectionItemPicker<SingleUnityLayer> Layers => layers;
    }
}
