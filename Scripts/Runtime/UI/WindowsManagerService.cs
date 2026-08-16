using UnityEngine;
#if UIMANAGER_AVAILABLE
using BrunoMikoski.ServicesLocation;
using BrunoMikoski.UIManager;
#endif

namespace BrunoMikoski.Core.DevelopmentTools.UI
{
#if UIMANAGER_AVAILABLE
    
    [ServiceImplementation(Name = "WindowsManager", Type = typeof(WindowsManager))]
    //We want the resolution of the multiple WindowsManager to be the first thing to happen after the service locator is initialized
    [DefaultExecutionOrder(-900)]
    public class WindowsManagerService : WindowsManager, IOnServiceRegistered
    {
        [SerializeField]
        private bool transferWindowsToMainService = true;

        private ServiceReference<WindowsManagerService> _windowService = new();

        //Hijacking the Awake to make sure we don't have multiple WindowsManager in the scene
        protected override void Awake()
        {
            if (_windowService.Exists)
            {
                if (_windowService.Reference == this)
                    return;
                
                if (transferWindowsToMainService)
                    _windowService.Reference.InitializeHierarchyWindows(transform);
                
                Destroy(this.gameObject);
            }
        }

        void IOnServiceRegistered.OnRegisteredOnServiceLocator(ServiceLocator serviceLocator)
        {
            Initialize();
            LoadInitialWindows();
        }
    }
#endif
}
