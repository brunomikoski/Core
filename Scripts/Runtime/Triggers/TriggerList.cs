using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    [Serializable]
    public class TriggerList
    {
        [SerializeField]
        private List<TriggerData> triggerDatas = new List<TriggerData>();

        public List<TriggerData> TriggerDatas => triggerDatas;

        public void StartTriggers(object source)
        {
            for (int i = 0; i < triggerDatas.Count; i++)
            {
                if (triggerDatas[i].Triggerable == null)
                    continue;

                triggerDatas[i].Triggerable.StartTrigger(source);
            }
        }

        public void StopTriggers()
        {
            for (int i = 0; i < triggerDatas.Count; i++)
            {
                if (triggerDatas[i].Triggerable == null)
                    continue;

                triggerDatas[i].Triggerable.StopTrigger();
            }
        }

        public void AddTriggerable(Triggerable triggerable)
        {
            TriggerData triggerData = new TriggerData(triggerable);
            triggerDatas.Add(triggerData);
        }

        public void RemoveTriggerable(Triggerable triggerable)
        {
            for (int i = triggerDatas.Count - 1; i >= 0; i--)
            {
                if (triggerDatas[i].Triggerable == triggerable)
                    triggerDatas.RemoveAt(i);
            }
        }

        public bool IsConnected(Triggerable triggerable)
        {
            for (int i = 0; i < triggerDatas.Count; i++)
            {
                if (triggerDatas[i].Triggerable == triggerable)
                    return true;
            }

            return false;
        }
    }
}