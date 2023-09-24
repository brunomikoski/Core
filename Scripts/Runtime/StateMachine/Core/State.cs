
using System.Threading;
#if UNITASK_AVAILABLE
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.Core.StateMachines
{
    public class State<TStateID> : StateBase where TStateID : StateID
    {
        public new TStateID StateID => (TStateID) stateID;
        public new StateMachine<TStateID> StateMachine => (StateMachine<TStateID>)stateMachine;

        public State(TStateID targetStateID) : base(targetStateID)
        {
        }
    }
    
    public class StateBase
    {
        protected StateMachineBase stateMachine;
        public StateMachineBase StateMachine => stateMachine;

        protected StateID stateID;
        public StateID StateID => stateID;

#if UNITASK_AVAILABLE
        private CancellationTokenSource exitCancellationTokenSource;
#endif
        
        protected StateBase(StateID targetStateID)
        {
            stateID = targetStateID;
        }

        internal void Internal_Enter()
        {
#if UNITY_EDITOR
            StateMachineTracker.TrackEnterState(stateMachine, this);
#endif
            Enter();
#if UNITASK_AVAILABLE
            exitCancellationTokenSource = new CancellationTokenSource();
            CancellationTokenSource cancellationToken =
                CancellationTokenSource.CreateLinkedTokenSource(exitCancellationTokenSource.Token, stateMachine.GetCancellationTokenOnDestroy());
            
            Enter_Async(cancellationToken).Forget();
#endif
            stateMachine.DispatchEvent(StateMachineEvent.StateEnter, stateID);
        }
        
        internal void Internal_Exit()
        {
#if UNITY_EDITOR
            StateMachineTracker.TrackExitState(stateMachine, this);
#endif
#if UNITASK_AVAILABLE
            if (exitCancellationTokenSource != null)
            {
                exitCancellationTokenSource.Cancel();
                exitCancellationTokenSource.Dispose();
            }
#endif
            Exit();
            
            stateMachine.DispatchEvent(StateMachineEvent.StateExit, stateID);
        }


        protected virtual void Enter()
        {
        }
        
#if UNITASK_AVAILABLE
        protected virtual UniTaskVoid Enter_Async(CancellationTokenSource cancellationToken)
        {
            return default;
        }
#endif

        protected virtual void Exit()
        {
        }

        internal virtual void Update()
        {
        }

        internal virtual void SetStateMachine(StateMachineBase targetStateMachine)
        {
            stateMachine = targetStateMachine;
        }
    }
}
