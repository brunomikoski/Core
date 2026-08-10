using System;
using System.Collections;
using UnityEngine;
#if UNITASK_ENABLED
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.Core.DevelopmentTools.Routines
{
    public class Routine : CustomYieldInstruction
    {
        public readonly IEnumerator Enumerator;
        private readonly MonoBehaviour monoBehaviour;
        public MonoBehaviour MonoBehaviour => monoBehaviour;

        internal Coroutine UnityCoroutine;

        private bool isRunning;
        public bool IsRunning => isRunning;

        private bool monoBehaviourDependent;
        
        private bool isPaused;
        public bool IsPaused => isPaused;

        public bool IsStopped => !isRunning;

        private bool isCompleted;
        public bool IsCompleted => isCompleted;


        public override bool keepWaiting => isRunning;
        
        private DateTime finishedTime;
        public DateTime FinishedTime => finishedTime;

        private DateTime stoppedTime;
        public DateTime StoppedTime => stoppedTime;

        public Routine(IEnumerator enumerator, MonoBehaviour monoBehaviour, bool usingGameObjectLifetime)
        {
            Enumerator = enumerator;
            this.monoBehaviour = monoBehaviour;
            monoBehaviourDependent = usingGameObjectLifetime;
        }

        public void Start()
        {
            isRunning = true;
        }

        public void Finished()
        {
            isRunning = false;
            isCompleted = true;
#if UNITY_EDITOR && DEBUG
            finishedTime = DateTime.UtcNow;
#endif
        }

        public void Pause()
        {
            isPaused = true;
        }

        public void Unpause()
        {
            isPaused = false;
        }

        public void TogglePause()
        {
            if (isPaused)
                Unpause();
            else
                Pause();
        }

        public bool IsValid()
        {
            if (monoBehaviourDependent)
                return monoBehaviour != null && monoBehaviour.gameObject != null;
            return true;
        }

        public void Stop()
        {
            isRunning = false;
            stoppedTime = DateTime.UtcNow;
        }
#if UNITASK_ENABLED        
        public async UniTask ToUniTask()
        {
            await UniTask.WaitUntil(() => isCompleted);
        }
#endif
    }
}
