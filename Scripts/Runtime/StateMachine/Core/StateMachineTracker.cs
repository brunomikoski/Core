using System;
using System.Collections.Generic;
using System.Diagnostics;
using BrunoMikoski.Core.Routines;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{

    public class StepData
    {
        public string DisplayName;
        public StackTrace EnterStackTrace;
        public StackTrace ExitStackTrace;
        public DateTime EnterTime;
        public DateTime ExitTime;
    }
    public class StateData : StepData
    {
    }

    public class TransitionData : StepData
    {
    }
    
    public class StateMachineState
    {
        public List<StepData> History = new();

        public StateData CurrentState;
        public TransitionData CurrentTransition;
        
        public List<string> AvailableNextStates;
    }

    public static class StateMachineTracker
    {
        private static Dictionary<StateMachineBase, StateMachineState> stateMachineToState = new();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SupportDomainReload()
        {
            stateMachineToState.Clear();
        }

        public static void TrackEnterState(StateMachineBase stateMachine, StateBase targetState)
        {
            StackTrace stackTrace = new StackTrace(5, true);

            StateMachineState stateMachineState = GetOrCreateStateMachineState(stateMachine);

            stateMachineState.CurrentState = new StateData
            {
                DisplayName = $"{targetState.StateID.name}",
                EnterStackTrace = stackTrace,
                EnterTime = DateTime.Now
            };

            stateMachineState.AvailableNextStates = GetAvailableNextSteps(stateMachine, targetState);
        }

        public static void TrackExitState(StateMachineBase stateMachine, StateBase targetState)
        {
            StackTrace stackTrace = new StackTrace(4, true);

            StateMachineState stateMachineState = GetOrCreateStateMachineState(stateMachine);

            stateMachineState.CurrentState.ExitStackTrace = stackTrace;
            stateMachineState.CurrentState.ExitTime = DateTime.Now;

            stateMachineState.History.Add(stateMachineState.CurrentState);
            stateMachineState.CurrentState = null;
        }

        public static void TrackTransitionBegin(StateMachineBase stateMachine, TransitionBase targetTransition)
        {
            StackTrace stackTrace = new StackTrace(4, true);

            StateMachineState stateMachineState = GetOrCreateStateMachineState(stateMachine);

            stateMachineState.CurrentTransition = new TransitionData()
            {
                DisplayName = $"{targetTransition.FromState.name} -> {targetTransition.ToState.name}",
                EnterStackTrace = stackTrace,
                EnterTime = DateTime.Now
            };

        }

        public static void TrackTransitionEnd(StateMachineBase stateMachine, TransitionBase targetTransition)
        {
            StackTrace stackTrace = new StackTrace(2, true);
            StateMachineState stateMachineState = GetOrCreateStateMachineState(stateMachine);
            
            stateMachineState.CurrentTransition.ExitStackTrace = stackTrace;
            stateMachineState.CurrentTransition.ExitTime = DateTime.Now;
            
            stateMachineState.History.Add(stateMachineState.CurrentTransition);
            stateMachineState.CurrentTransition = null;
        }
        
        private static List<string> GetAvailableNextSteps(StateMachineBase stateMachine, StateBase targetState)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < stateMachine.AvailableTransitions.Count; i++)
            {
                TransitionBase transitionBase = stateMachine.AvailableTransitions[i];
                if (transitionBase.FromState != targetState.StateID)
                    continue;

                if (!result.Contains(transitionBase.ToState.name))
                    result.Add(transitionBase.ToState.name);
            }

            foreach (StateBase stateBase in stateMachine.AvailableStates)
            {
                if (!stateBase.StateID.FromAnyState)
                    continue;
                
                if (!result.Contains(stateBase.StateID.name))
                    result.Add(stateBase.StateID.name);
            }

            return result;
        }

        
        
        private static StateMachineState GetOrCreateStateMachineState(StateMachineBase stateMachine)
        {
            if (stateMachineToState.TryGetValue(stateMachine, out StateMachineState resultState))
                return resultState;
            
            
            resultState = new StateMachineState();
            stateMachineToState.Add(stateMachine, resultState);
            return resultState;
        }

        public static bool TryGetStateMachineState(StateMachineBase stateMachine, out StateMachineState stateMachineState)
        {
            return stateMachineToState.TryGetValue(stateMachine, out stateMachineState);
        }

        public static void Forget(StateMachineBase stateMachineBase)
        {
            stateMachineToState.Remove(stateMachineBase);
        }
    }
}