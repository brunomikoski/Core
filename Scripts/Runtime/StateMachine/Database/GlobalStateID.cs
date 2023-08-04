using System;

namespace BrunoMikoski.Core.StateMachines
{
    public class GlobalStateID : StateID
    {
        public event Action OnEnterEvent
        {
            add => StateMachineRegistry.SubscribeToGlobalEvent(StateMachineEvent.StateEnter, this, value);
            remove => StateMachineRegistry.UnsubscribeFromGlobalEvent(StateMachineEvent.StateEnter, this, value);
        }
        
        public event Action OnExitEvent
        {
            add => StateMachineRegistry.SubscribeToGlobalEvent(StateMachineEvent.StateExit, this, value);
            remove => StateMachineRegistry.UnsubscribeFromGlobalEvent(StateMachineEvent.StateExit, this, value);
        }
        
        public event Action OnTransitionOutEvent
        {
            add => StateMachineRegistry.SubscribeToGlobalEvent(StateMachineEvent.BeginTransitionFrom, this, value);
            remove => StateMachineRegistry.UnsubscribeFromGlobalEvent(StateMachineEvent.BeginTransitionFrom, this, value);
        }

        public event Action OnTransitionInEvent
        {
            add => StateMachineRegistry.SubscribeToGlobalEvent(StateMachineEvent.BeginTransitionTo, this, value);
            remove => StateMachineRegistry.UnsubscribeFromGlobalEvent(StateMachineEvent.BeginTransitionTo, this, value);
        }

    }
}