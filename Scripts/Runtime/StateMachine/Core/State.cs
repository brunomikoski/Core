
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

        private CancellationTokenSource exitCancellationTokenSource;
        private CancellationTokenSource enterLinkedCancellationTokenSource;
        
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
            exitCancellationTokenSource = new CancellationTokenSource();
            enterLinkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(exitCancellationTokenSource.Token, stateMachine.GetCancellationTokenOnDestroy());
            Enter_Async(enterLinkedCancellationTokenSource.Token).Forget();
            stateMachine.DispatchEvent(StateMachineEvent.StateEnter, stateID);
        }
        
        internal void Internal_Exit()
        {
#if UNITY_EDITOR
            StateMachineTracker.TrackExitState(stateMachine, this);
#endif
            if (exitCancellationTokenSource != null)
            {
                exitCancellationTokenSource.Cancel();
                exitCancellationTokenSource.Dispose();
                exitCancellationTokenSource = null;
            }
            if (enterLinkedCancellationTokenSource != null)
            {
                enterLinkedCancellationTokenSource.Dispose();
                enterLinkedCancellationTokenSource = null;
            }
            
            Exit();
            
            stateMachine.DispatchEvent(StateMachineEvent.StateExit, stateID);
        }


        protected virtual void Enter()
        {
        }
        
        protected virtual UniTask Enter_Async(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

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
