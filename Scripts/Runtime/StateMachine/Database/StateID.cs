using BrunoMikoski.ScriptableObjectCollections;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    public class StateID : ScriptableObjectCollectionItem
    {
        [Header("Settings")]
        [SerializeField]
        private bool initialState;
        public bool InitialState => initialState;
        
        public bool FromAnyState => AllowedTransitions.Count == 0;

        public bool AllowSelfTransition => AllowedTransitions.Contains(this);
        
        [field: SerializeField]
        public CollectionItemPicker<StateID> AllowedTransitions { get; private set; } = new();
    }
}
