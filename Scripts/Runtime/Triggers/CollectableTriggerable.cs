using System;
using BrunoMikoski.Core.Events;
using BrunoMikoski.Core.PhysX;
using UnityEngine;

namespace BrunoMikoski.Core.Triggers
{
    public class CollectableTriggerable : Triggerable
    {
        [SerializeField]
        protected TriggerVolumeBase triggerVolume;

        private readonly SafeEvent _authorityDestroy = new();
        public event Action AuthorityDestroyEvent
        {
            add => _authorityDestroy.AddListener(value);
            remove => _authorityDestroy.RemoveListener(value);
        }

        protected override void StartTriggerInternal(object source)
        {
            base.StartTriggerInternal(source);
            _authorityDestroy.Dispatch();
        }
    }
}
