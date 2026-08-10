using System;
using BrunoMikoski.Core.SafeEvents;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public abstract class Triggerable : MonoBehaviour
    {
        [Header("Triggerable")]
        [SerializeField]
        protected bool startsActive = false;
        [SerializeField] 
        private bool invertTrigger = false;

        public bool StartsActive { get { return startsActive; } }
        public bool InvertTrigger => invertTrigger;

        public virtual bool CanTriggerWhileAlreadyTriggered { get; protected set; }

        //Used to prevent weird execution order issues
        private bool isInitialized;
        public virtual bool CanBeTriggered => true;

        /// <summary>
        /// This field holds the internal state of the trigger. So if the trigger is
        /// inverted, and the trigger got a StartTrigger message, IsActive will be false.
        /// If the trigger is initialized and the trigger is inverted, IsActive will be true.
        /// </summary>
        public bool IsActive { get; private set; }

        private readonly SafeEvent<Triggerable> _onStartTriggerEvent = new();
        public event Action<Triggerable> OnStartTriggerEvent
        {
            add => _onStartTriggerEvent.AddListener(value);
            remove => _onStartTriggerEvent.RemoveListener(value);
        }

        private readonly SafeEvent<Triggerable> _onStopTriggerEvent = new();
        public event Action<Triggerable> OnStopTriggerEvent
        {
            add => _onStopTriggerEvent.AddListener(value);
            remove => _onStopTriggerEvent.RemoveListener(value);
        }

        private void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            if (startsActive)
            {
                ForceStartTrigger(this);
            }
            else
            {
                IsActive = invertTrigger;
            }
        }

        protected virtual void Start()
        {
            Initialize();
        }

        public void StartTrigger(object source)
        {
            Initialize();

            if (!CanStartTrigger())
                return;

            ForceStartTrigger(source);
        }

        private void ForceStartTrigger(object source)
        {
            if (!invertTrigger)
            {
                // Set to true before sending the event is
                // important for ordering reasons
                IsActive = true;
                StartTriggerInternal(source);
                _onStartTriggerEvent.Invoke(this);
            }
            else
            {
                // Set to false before sending the event is
                // important for ordering reasons
                IsActive = false;
                StopTriggerInternal(source);
                _onStopTriggerEvent.Invoke(this);
            }
        }

        public virtual bool CanStartTrigger()
        {
            return enabled && ((IsActive == invertTrigger) || CanTriggerWhileAlreadyTriggered) && CanBeTriggered;
        }

        protected virtual void StartTriggerInternal(object source)
        {
        }

        public void StopTrigger(object source = null)
        {
            if (source == null)
                source = this;

            Initialize();
            if (!CanStopTrigger())
                return;

            ForceStopTrigger(source);
        }

        protected void ForceStopTrigger(object source = null)
        {
            if (source == null)
                source = this;
            if (!invertTrigger)
            {
                // Set to false before sending the event is
                // important for ordering reasons
                IsActive = false;
                StopTriggerInternal(source);
                _onStopTriggerEvent.Invoke(this);
            }
            else
            {
                // Set to true before sending the event is
                // important for ordering reasons
                IsActive = true;
                StartTriggerInternal(source);
                _onStartTriggerEvent.Invoke(this);
            }
        }

        public virtual bool CanStopTrigger()
        {
            return (IsActive != invertTrigger) || CanTriggerWhileAlreadyTriggered;
        }

        protected virtual void StopTriggerInternal(object source)
        {
        }

        public void ToggleTrigger(object source)
        {
            Initialize();
            if (IsActive != invertTrigger)
                StopTrigger(source);
            else
                StartTrigger(source);
        }

        [ContextMenu("Start Trigger")]
        private void ContextMenuStartTrigger()
        {
            StartTrigger(this);
        }

        [ContextMenu("Start Trigger", true)]
        private bool CanUseContextMenuStartTrigger()
        {
            return Application.isPlaying && CanStartTrigger();
        }

        [ContextMenu("Stop Trigger")]
        private void ContextMenuStopTrigger()
        {
            StopTrigger();
        }

        [ContextMenu("Stop Trigger", true)]
        private bool CanUseContextMenuStopTrigger()
        {
            return Application.isPlaying && CanStopTrigger();
        }

    }
}
