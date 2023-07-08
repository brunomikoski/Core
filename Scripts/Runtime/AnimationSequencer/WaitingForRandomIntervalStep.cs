#if DOTWEEN_ENABLED && SEQUENCER_AVAILABLE
using System;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BrunoMikoski.AnimationSequencer
{
    [Serializable]
    public sealed class WaitForRandomIntervalStep : AnimationStepBase
    {
        public override string DisplayName => "Wait for Random Interval";

        [SerializeField]
        private float intervalMin = 0;
        public float IntervalMin
        {
            get => intervalMin;
            set => intervalMin = value;
        }

        [SerializeField]
        private float intervalMax = 1;
        public float IntervalMax
        {
            get => intervalMax;
            set => intervalMax = value;
        }

        
        public override void AddTweenToSequence(Sequence animationSequence)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.SetDelay(Delay);

            sequence.AppendInterval(Random.Range(intervalMin, intervalMax));
            
            if (FlowType == FlowType.Join)
                animationSequence.Join(sequence);
            else
                animationSequence.Append(sequence);        }

        public override void ResetToInitialState()
        {
        }

        public override string GetDisplayNameForEditor(int index)
        {
            return $"{index}. Wait between {intervalMin} to {intervalMax} seconds";
        }
    }
}
#endif