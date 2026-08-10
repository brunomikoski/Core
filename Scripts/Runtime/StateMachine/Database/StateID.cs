using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.StateMachines
{
    public partial class StateID : ScriptableObjectCollectionItem
    {
        [Header("Settings")]
        [SerializeField]
        private bool initialState;
        public bool InitialState => initialState;
        
        [SerializeField]
        private bool fromAnyState;
        public bool FromAnyState => fromAnyState;

        [SerializeField]
        private bool allowSelfTransition;
        public bool AllowSelfTransition => allowSelfTransition;
    }
}
