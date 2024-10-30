using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.Routines
{
    public sealed class CoroutineRunner 
    {
        private static bool initialized;
        private static bool isQuitting;

        private static CoroutineRunnerMonoBehaviour ROUTINE_RUNNER;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SupportDomainReload()
        {
            ROUTINE_RUNNER = null;
            initialized = false;
            isQuitting = false;
        }

        private static void Initialize()
        {
            if (initialized)
                return;

            if (isQuitting)
                return;

            ROUTINE_RUNNER = Object.FindObjectOfType<CoroutineRunnerMonoBehaviour>();
            if (ROUTINE_RUNNER == null)
            {
                GameObject coroutineGameObject = new(nameof(CoroutineRunnerMonoBehaviour));
                ROUTINE_RUNNER = coroutineGameObject.AddComponent<CoroutineRunnerMonoBehaviour>();
                Object.DontDestroyOnLoad(coroutineGameObject);
            }

            initialized = true;
        }

        public static void StartIfNotRunning(ref Routine routine)
        {
            Initialize();
            
            if (isQuitting)
                return;

            if (routine == null)
                throw new NullReferenceException("The target routine is null");

            if (routine.IsRunning)
                return;

            StartRoutine_Internal(routine);
        }

        public static void StopAndClear(ref Routine routine)
        {
            Initialize();
            
            if (isQuitting)
                return;

            if (routine == null)
                return;

            routine.Stop();
            routine = null;
        }
        public static void StopAndStart(ref Routine routine, IEnumerator enumerator, MonoBehaviour ownerBehaviour = null)
        {
            Initialize();
            
            if (isQuitting)
                return;

            StopAndClear(ref routine);
            routine = StartCoroutine(enumerator, ownerBehaviour);
        }

        public static Routine StartCoroutine(IEnumerator targetEnumerator, MonoBehaviour ownerBehaviour = null)
        {
            Initialize();
            
            MonoBehaviour behaviour = ROUTINE_RUNNER;
            if (ownerBehaviour != null)
                behaviour = ownerBehaviour;

            Routine routine = new Routine(targetEnumerator, behaviour,
                ownerBehaviour != ROUTINE_RUNNER);

            StartRoutine_Internal(routine);
            return routine;
        }

        private static void StartRoutine_Internal(Routine targetRoutine)
        {
#if UNITY_EDITOR
            CoroutineServiceTracker.TrackRoutine(targetRoutine);
#endif
            ROUTINE_RUNNER.StartCoroutine(RunRoutineEnumerator(targetRoutine));
        }

        private static IEnumerator RunRoutineEnumerator(Routine routine)
        {
            routine.Start();
            IEnumerator enumerator = routine.Enumerator;
            while (routine.IsRunning)
            {
                if (routine.IsPaused)
                {
                    yield return null;
                    continue;
                }

                if (enumerator != null && routine.IsValid() && enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
                else
                {
                    routine.Finished();
                }
            }
        }

        public static void StopAll()
        {
            if (ROUTINE_RUNNER == null)
                return;
            
            ROUTINE_RUNNER.StopAllCoroutines();
        }

        public static void OnApplicationQuit()
        {
            StopAll();
            isQuitting = true;
        }

        public static void OnRunnerDestroyed()
        {
            StopAll();
        }
    }
}