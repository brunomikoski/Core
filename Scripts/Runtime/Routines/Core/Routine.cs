using System.Collections;
using UnityEngine;
#if UNITASK_ENABLED
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.Core.Routines
{
    public class Routine : CustomYieldInstruction
    {
        public readonly IEnumerator Enumerator;
        private readonly MonoBehaviour monoBehaviour;

        private bool isRunning;
        public bool IsRunning => isRunning;

        private bool monoBehaviourDependent;
        
        private bool isPaused;
        public bool IsPaused => isPaused;

        public bool IsStopped => !isRunning;

        private bool isCompleted;
        public bool IsCompleted => isCompleted;


        public override bool keepWaiting => isRunning;

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
        }
#if UNITASK_ENABLED        
        public async UniTask ToUniTask()
        {
            await UniTask.WaitUntil(() => isCompleted);
        }
#endif
    }
}
