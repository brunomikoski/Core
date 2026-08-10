using System;
using System.Collections.Generic;
using BrunoMikoski.Core.SafeEvents;
#if SIMPLEPOOL_AVAILABLE
using BrunoMikoski.Pooling;
#endif
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public abstract class Trigger : MonoBehaviour, ITrigger
#if SIMPLEPOOL_AVAILABLE
        , IOnSpawn, IOnDespawn
#endif
    {
        [SerializeField, HideInInspector]
        private List<Triggerable> triggerables = new List<Triggerable>();
        [SerializeField]
        protected TriggerList triggerList = new TriggerList();

        public List<TriggerData> Triggerables => triggerList.TriggerDatas;

        [SerializeField]
        protected bool onlyTriggerOnce;

        protected readonly SafeEvent<Trigger> _triggerStart = new();
        public event Action<Trigger> OnTriggerStartEvent
        {
            add => _triggerStart.AddListener(value);
            remove => _triggerStart.RemoveListener(value);
        }

        private readonly SafeEvent<Trigger> _triggerStop = new();
        public event Action<Trigger> OnTriggerStopEvent
        {
            add => _triggerStop.AddListener(value);
            remove => _triggerStop.RemoveListener(value);
        }


        protected bool IsTriggerActive;

        protected bool HasBeenTriggered;
        

        protected virtual void OnDestroy()
        {
            if (MonoBehaviourService.IsQuitting)
                return;

            triggerList.CancelScheduledTriggers();
        }

        protected void StartTrigger()
        {
            StartTrigger(this);
        }

        protected void StartTrigger(object source)
        {
            if (onlyTriggerOnce && HasBeenTriggered)
                return;

            IsTriggerActive = true;
            triggerList.StartTriggers(source);

            HasBeenTriggered = true;
            _triggerStart.Invoke(this);
        }


        protected void StopTrigger()
        {
            IsTriggerActive = false;
            triggerList.StopTriggers();

            _triggerStop.Invoke(this);
        }

        public void AddTriggerable(Triggerable triggerable, float delay = 0.0f)
        {
            triggerList.AddTriggerable(triggerable, delay);
        }

        public void RemoveTriggerable(Triggerable triggerable)
        {
            triggerList.RemoveTriggerable(triggerable);
        }

        public bool IsConnected(Triggerable triggerable)
        {
            EnsureValidated();
            return triggerList.IsConnected(triggerable);
        }


        public void EnsureValidated()
        {
            if (!Application.isPlaying)
                OnValidate();
        }

        protected virtual void OnValidate()
        {
            if (!gameObject.scene.IsValid())
                return;
            
            for (int i = 0; i < triggerables.Count; i++)
            {
                Triggerable triggerable = triggerables[i];

                if (triggerable == null)
                    continue;

                triggerList.AddTriggerable(triggerable);
                triggerables[i] = null;
            }

            for (int i = 0; i < triggerList.TriggerDatas.Count; i++)
            {
                if ( triggerList.TriggerDatas[i].Triggerable == null)
                {
                    Debug.LogWarningFormat(
                        this, "Triggerable is unassigned at index {0}, object "
                              + "{1}, scene {2}", i, transform.GetPath(), gameObject.scene.path);
                }
            }
        }

#if SIMPLEPOOL_AVAILABLE
        void IOnDespawn.OnDespawn()
        {
            if (IsTriggerActive)
                StopTrigger();
        }

        void IOnSpawn.OnSpawn()
        {
            if (onlyTriggerOnce)
                HasBeenTriggered = false;
        }
#endif
    }
}
