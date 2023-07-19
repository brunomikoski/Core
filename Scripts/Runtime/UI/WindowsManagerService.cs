#if UI_MANAGER_AVAILABLE
using BrunoMikoski.ServicesLocation;
using UnityEngine;
using BrunoMikoski.UIManager;

namespace BrunoMikoski.Core.UI
{
    [ServiceImplementation(Name = "WindowsManager", Type = typeof(WindowsManager))]
    //We want the resolution of the multiple WindowsManager to be the first thing to happen after the service locator is initialized
    [DefaultExecutionOrder(-900)]
    public class WindowsManagerService : WindowsManager, IOnServiceRegistered
    {
        [SerializeField]
        private bool transferWindowsToMainService = true;
        
        //Hijacking the Awake to make sure we don't have multiple WindowsManager in the scene
        protected override void Awake()
        {
            if (ServiceLocator.Instance.TryGetInstance(out WindowsManager windowsManager))
            {
                if (windowsManager == this)
                    return;
                
                if (transferWindowsToMainService)
                    windowsManager.InitializeHierarchyWindows(transform);
                
                //Destroying parent to take care of the camera as well.
                if(transform.parent.GetComponentInChildren<Camera>() != null)
                    Destroy(transform.parent.gameObject);
                else
                    Destroy(gameObject);
            }
        }

        void IOnServiceRegistered.OnRegisteredOnServiceLocator(ServiceLocator serviceLocator)
        {
            Initialize();
            LoadInitialWindows();
        }
    }
}
#endif