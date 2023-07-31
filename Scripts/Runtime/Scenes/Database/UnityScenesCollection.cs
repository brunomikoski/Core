using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using System.Collections.Generic;

namespace BrunoMikoski.Core.ScenesReferences
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Create UnityScenesCollection", fileName = "UnityScenesCollection", order = 0)]
    public class UnityScenesCollection : ScriptableObjectCollection<UnityScene>
    {
    }
}
