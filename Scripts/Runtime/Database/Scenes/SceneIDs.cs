using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using System.Collections.Generic;

namespace BrunoMikoski.Playground.ScenesReferences
{
    [CreateAssetMenu(menuName = "ScriptableObject Collection/Collections/Create SceneIDs", fileName = "SceneIDs", order = 0)]
    public class SceneIDs : ScriptableObjectCollection<SceneID>
    {
    }
}
