using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.DevelopmentTools.Routines
{
    public sealed class CoroutineRunner 
    {
        private static bool initialized;
        private static bool isQuitting;
        private static bool tearDownInProgress;

        private static CoroutineRunnerMonoBehaviour ROUTINE_RUNNER;

        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void HookQuitting()
        {
            Application.quitting += () => { isQuitting = true; };
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SupportDomainReload()
        {
            ROUTINE_RUNNER = null;
            initialized = false;
            isQuitting = false;
            tearDownInProgress = false;
        }

        private static void Initialize()
        {
            if (initialized)
                return;

            if (isQuitting || tearDownInProgress)
                return;

#if UNITY_6000_0_OR_NEWER
            ROUTINE_RUNNER = Object.FindAnyObjectByType<CoroutineRunnerMonoBehaviour>();
#else
            ROUTINE_RUNNER = Object.FindObjectOfType<CoroutineRunnerMonoBehaviour>();
#endif

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
            if (isQuitting || tearDownInProgress)
                return;

            Initialize();

            if (routine == null)
                throw new NullReferenceException("The target routine is null");

            if (routine.IsRunning)
                return;

            StartRoutine_Internal(routine);
        }
        
        public static void StartIfNotRunning(ref Routine routine, IEnumerator enumerator, MonoBehaviour ownerBehaviour = null)
        {
            if (isQuitting || tearDownInProgress)
                return;

            Initialize();

            if (routine != null && routine.IsRunning)
                return;
            
            routine = StartCoroutine(enumerator, ownerBehaviour);
        }

        public static void StopAndClear(ref Routine routine)
        {
            if (isQuitting || tearDownInProgress)
            {
                routine = null;
                return;
            }

            Initialize();

            if (routine == null)
                return;

            routine.Stop();

            if (routine.UnityCoroutine != null)
            {
                ROUTINE_RUNNER.StopCoroutine(routine.UnityCoroutine);
                routine.UnityCoroutine = null;
            }

            routine = null;
        }
        public static void StopAndStart(ref Routine routine, IEnumerator enumerator, MonoBehaviour ownerBehaviour = null)
        {
            if (isQuitting || tearDownInProgress)
            {
                routine = null;
                return;
            }

            Initialize();

            StopAndClear(ref routine);
            routine = StartCoroutine(enumerator, ownerBehaviour);
        }

        public static Routine StartCoroutine(IEnumerator targetEnumerator, MonoBehaviour ownerBehaviour = null)
        {
            if (isQuitting || tearDownInProgress)
                return null;

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
            targetRoutine.UnityCoroutine = ROUTINE_RUNNER.StartCoroutine(RunRoutineEnumerator(targetRoutine));
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

        public static Routine WaitForOneFrame(Action callback, MonoBehaviour owner = null)
        {
            return WaitForFrames(1, callback, owner);
        }
        public static IEnumerator WaitForFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        public static Routine WaitForFrames(int frames, Action callback, MonoBehaviour owner = null)
        {
            return StartCoroutine(WaitForFramesRoutine(frames, callback), owner);
        }

        private static IEnumerator WaitForFramesRoutine(int frames, Action callback)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }

            callback?.Invoke();
        }

        #region WaitForSeconds
        public static Routine WaitForSeconds(float seconds, MonoBehaviour owner = null)
        {
            return StartCoroutine(WaitForSecondsRoutine(seconds, false, null), owner);
        }

        public static Routine WaitForSeconds(float seconds, Action callback, MonoBehaviour owner = null)
        {
            return StartCoroutine(WaitForSecondsRoutine(seconds, false, callback), owner);
        }
        public static Routine WaitForSeconds(float seconds, bool useRealTime, Action callback, MonoBehaviour owner = null)
        {
            return StartCoroutine(WaitForSecondsRoutine(seconds, useRealTime, callback), owner);
        }

        private static IEnumerator WaitForSecondsRoutine(float seconds, bool useRealTime, Action callback)
        {
            if (!useRealTime)
                yield return new WaitForSeconds(seconds);
            else
                yield return new WaitForRealSeconds(seconds);

            if (callback != null)
                callback();
        }
#endregion WaitForSeconds

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
            tearDownInProgress = true;
            StopAll();
            ROUTINE_RUNNER = null;
            initialized = false;
        }
    }
}
