using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public partial class Tag : ScriptableObjectCollectionItem, ISOCColorizedItem
    {
        [SerializeField]
        private Color labelBackgroundColor = Color.black;

        public Color LabelColor => labelBackgroundColor;
    }
}
