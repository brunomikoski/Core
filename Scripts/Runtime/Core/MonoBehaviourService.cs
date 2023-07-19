using System;
using System.Collections.Generic;
using BrunoMikoski.ServicesLocation;
using UnityEngine;


namespace BrunoMikoski.Core
{
	// ReSharper disable StaticMemberInGenericType

    [ServiceImplementation]
    public sealed class MonoBehaviourService : MonoBehaviour
    {
        private List<IOnUpdate> updateListeners = new ();
        private List<ILateUpdate> lateUpdateListeners = new ();
        private List<IFixedUpdate> fixedUpdateListeners = new ();

        private static bool isQuitting;
        public static bool IsQuitting
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return true;
#endif
                return isQuitting;
            }
        }
        
        public event Action OnUpdate;
        public event Action OnLateUpdate;
        public event Action OnFixedUpdate;

        
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            isQuitting = false;
        }
        
        private void Update()
        {
            DispatchUpdate();
        }

        private void LateUpdate()
        {
            DispatchLateUpdate();
        }

        private void FixedUpdate()
        {
            DispatchFixedUpdate();
        }
        
        private void DispatchLateUpdate()
        {
            for (int i = 0; i < lateUpdateListeners.Count; i++)
                lateUpdateListeners[i].OnLateUpdate();
            
            OnLateUpdate?.Invoke();
        }

        private void DispatchFixedUpdate()
        {
            for (int i = 0; i < fixedUpdateListeners.Count; i++)
                fixedUpdateListeners[i].OnFixedUpdate();
            
            OnFixedUpdate?.Invoke();
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        private void DispatchUpdate()
        {
            for (int i = 0; i < updateListeners.Count; i++)
                updateListeners[i].OnUpdate();
            
            OnUpdate?.Invoke();
        }

        public void SubscribeToUpdateLoop(IOnUpdate onUpdateListener)
        {
            updateListeners.Add(onUpdateListener);
        }

        public void UnsubscribeFromUpdateLoop(IOnUpdate onUpdateListener)
        {
            updateListeners.Remove(onUpdateListener);
        }
        
        public void SubscribeToLateUpdateLoop(ILateUpdate onUpdateListener)
        {
            lateUpdateListeners.Add(onUpdateListener);
        }

        public void UnsubscribeFromLateUpdate(ILateUpdate onUpdateListener)
        {
            lateUpdateListeners.Remove(onUpdateListener);
        }
        
        public void SubscribeToFixedUpdateLoop(IFixedUpdate onUpdateListener)
        {
            fixedUpdateListeners.Add(onUpdateListener);
        }

        public void UnsubscribeFromFixedUpdateLoop(IFixedUpdate onUpdateListener)
        {
            fixedUpdateListeners.Remove(onUpdateListener);
        }

    }
}