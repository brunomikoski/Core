using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using System.Collections.Generic;

namespace BrunoMikoski.Core.Layers
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Create LayerCollection", fileName = "LayerCollection", order = 0)]
    public class LayerCollection : ScriptableObjectCollection<Layer>
    {
    }
}
