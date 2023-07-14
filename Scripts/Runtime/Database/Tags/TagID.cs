using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public partial class TagID : ScriptableObjectCollectionItem, ISOCColorizedItem
    {
        [SerializeField]
        private Color labelBackgroundColor = Color.black;

        public Color LabelColor => labelBackgroundColor;
    }
}
