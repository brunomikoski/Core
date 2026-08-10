using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Layers
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Create UnityLayerCollection", fileName = "UnityLayerCollection", order = 0)]
    public class UnityLayerCollection : ScriptableObjectCollection<UnityLayer>
    {
    }
}