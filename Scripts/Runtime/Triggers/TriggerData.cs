using System;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    [Serializable]
    public class TriggerData
    {
        [SerializeField] private Triggerable triggerable;

        public Triggerable Triggerable => triggerable;

        public TriggerData(Triggerable triggerable)
        {
            this.triggerable = triggerable;
        }
    }
}