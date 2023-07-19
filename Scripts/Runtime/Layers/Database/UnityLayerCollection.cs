using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using System.Collections.Generic;

namespace BrunoMikoski.Core.Layers
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Create UnityLayerCollection", fileName = "UnityLayerCollection", order = 0)]
    public class UnityLayerCollection : ScriptableObjectCollection<UnityLayer>
    {
    }
}
