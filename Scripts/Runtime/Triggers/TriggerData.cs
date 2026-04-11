using System;
using UnityEngine;

namespace BrunoMikoski.Core.Triggers
{
    [Serializable]
    public class TriggerData
    {
        [SerializeField] private float delay;
        [SerializeField] private Triggerable triggerable;

        public float Delay => delay;
        public Triggerable Triggerable => triggerable;

        public TriggerData(Triggerable triggerable, float delay)
        {
            this.triggerable = triggerable;
            this.delay = delay;
        }
    }
}