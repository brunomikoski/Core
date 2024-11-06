using System.Collections.Generic;
using BrunoMikoski.ServicesLocation;

namespace BrunoMikoski.Core
{
    [ServiceImplementation]
    public class TagRegistryService
    {
        private const int INITIAL_SIZE = 100;
        private List<TagController> tagControllers = new(INITIAL_SIZE);
        
        
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