using System.Collections.Generic;

namespace BrunoMikoski.Core
{
#if TAGMANAGER_ENABLED
    [ServiceImplementation]
#endif
    public class TagRegistryService
    {
        private const int INITIAL_SIZE = 100;
        private List<TagController> tagControllers = new List<TagController>(INITIAL_SIZE);
        
        
        public void RegisterTagController(TagController tagController)
        {
            tagControllers.Add(tagController);
        }
        
        public void UnregisterTagController(TagController tagController)
        {
            tagControllers.Remove(tagController);
        }
    }
}