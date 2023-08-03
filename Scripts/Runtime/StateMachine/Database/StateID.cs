using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    public class StateID : ScriptableObjectCollectionItem
    {
        [SerializeField]
        private bool initialState;
        public bool InitialState => initialState;
        
        [SerializeField]
        private bool fromAnyState;
        public bool FromAnyState => fromAnyState;
    }
}