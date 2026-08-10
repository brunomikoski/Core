using System;
using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.Core.DevelopmentTools.Routines;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    [Serializable]
    public class TriggerList
    {
        [SerializeField]
        private List<TriggerData> triggerDatas = new List<TriggerData>();

        public List<TriggerData> TriggerDatas => triggerDatas;
        private Routine triggerDelayedTriggerables;
        private int currentDelayedTriggerIndex;

        public void CancelScheduledTriggers()
        {
            CoroutineRunner.StopAndClear(ref triggerDelayedTriggerables);
            currentDelayedTriggerIndex = -1;
        }

        private bool HasDelays
        {
            get
            {
                for (int i = 0; i < triggerDatas.Count; i++)
                {
                    TriggerData data = triggerDatas[i];
                    if (data.Delay > 0.0f)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void StartTriggers(object source)
        {
            if (HasDelays)
            {
                CoroutineRunner.StopAndStart(ref triggerDelayedTriggerables, TriggerTriggerablesDelayedEnumerator(source));
            }
            else
            {
                TriggerTriggerables(source);
            }
        }

        private void TriggerTriggerables(object source)
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
            if (HasDelays)
            {
                CoroutineRunner.StopAndClear(ref triggerDelayedTriggerables);

                if (currentDelayedTriggerIndex != -1)
                {
                    for (int i = 0; i < currentDelayedTriggerIndex + 1; i++)
                    {
                        if (triggerDatas[i] == null)
                            continue;

                        triggerDatas[i].Triggerable.StopTrigger();
                    }

                    currentDelayedTriggerIndex = -1;
                }
            }
            else
            {
                StopTriggeringTriggerables();
            }
        }

        private IEnumerator TriggerTriggerablesDelayedEnumerator(object source)
        {
            currentDelayedTriggerIndex = -1;

            for (int i = 0; i < triggerDatas.Count; i++)
            {
                TriggerData trigger = triggerDatas[i];

                if (trigger.Triggerable == null)
                    continue;

                if (!trigger.Delay.Approximately(0.0f))
                    yield return new WaitForSeconds(trigger.Delay);

                triggerDatas[i].Triggerable.StartTrigger(source);
                currentDelayedTriggerIndex++;
            }
        }

        private void StopTriggeringTriggerables()
        {
            for (int i = 0; i < triggerDatas.Count; i++)
            {
                if (triggerDatas[i].Triggerable == null)
                    continue;

                triggerDatas[i].Triggerable.StopTrigger();
            }
        }

        public void AddTriggerable(Triggerable triggerable, float delay = 0.0f)
        {
            TriggerData triggerData = new TriggerData(triggerable, delay);
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