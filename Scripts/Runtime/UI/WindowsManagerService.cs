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
        protected bool transferWindowsToMainService = true;

        protected readonly ServiceReference<WindowsManagerService> windowService = new();

        //Hijacking the Awake to make sure we don't have multiple WindowsManager in the scene
        protected override void Awake()
        {
            if (windowService.Exists)
            {
                if (windowService.Reference == this)
                    return;

                if (transferWindowsToMainService)
                    windowService.Reference.InitializeHierarchyWindows(transform);

                Destroy(this.gameObject);
            }
        }

        void IOnServiceRegistered.OnRegisteredOnServiceLocator(ServiceLocator serviceLocator)
        {
            Initialize();
            LoadInitialWindows();
        }

    }
}
