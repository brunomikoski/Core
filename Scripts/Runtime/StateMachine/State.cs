using System;

namespace BrunoMikoski.Core.StateMachines
{
    public class State
    {
        public StateMachine StateMachine { get; internal set; }
        public Enum StateType { get; internal set; }

        public event Action OnEnterEvent;
        public event Action OnUpdateEvent;
        public event Action OnFixedUpdateEvent;
        public event Action OnLateUpdateEvent;
        public event Action OnExitEvent;

        public bool IsCurrentState
        {
            get;
            internal set;
        }

        public void Enter()
        {
            DispatchOnEnterEvent();
            EnterState();
        }

        // EnterState is for the children to override
        protected virtual void EnterState()
        {
        }

        public void Update()
        {
            DispatchOnUpdateEvent();
            UpdateState();
        }

        // UpdateState is for the children to override
        protected virtual void UpdateState()
        {
        }

        public void LateUpdate()
        {
            DispatchOnLateUpdateEvent();
            LateUpdateState();
        }

        // LateUpdateState is for the children to override
        protected virtual void LateUpdateState()
        {
        }

        public void FixedUpdate()
        {
            DispatchOnFixedUpdateEvent();
            FixedUpdateState();
        }

        // FixedUpdateState is for the children to override
        protected virtual void FixedUpdateState()
        {
        }

        public void Exit()
        {
            DispatchOnExitEvent();
            ExitState();
        }

        // ExitState is for the children to override
        protected virtual void ExitState()
        {
        }

        private void DispatchOnEnterEvent()
        {
            OnEnterEvent?.Invoke();
        }

        private void DispatchOnUpdateEvent()
        {
            OnUpdateEvent?.Invoke();
        }

        private void DispatchOnFixedUpdateEvent()
        {
            OnFixedUpdateEvent?.Invoke();
        }

        private void DispatchOnLateUpdateEvent()
        {
            OnLateUpdateEvent?.Invoke();
        }

        private void DispatchOnExitEvent()
        {
            OnExitEvent?.Invoke();
        }
    }
}
