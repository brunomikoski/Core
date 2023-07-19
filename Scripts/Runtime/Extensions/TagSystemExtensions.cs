using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public static class TagSystemExtensions
    {
        private static Dictionary<object, TagController> objectToTagManagerCache = new Dictionary<object, TagController>();

        public static TagController GetTagManager(this Component target)
        {
            return GetTagManager(target.gameObject);
        }

        public static TagController GetTagManager(this GameObject target)
        {
            if (objectToTagManagerCache.TryGetValue(target, out TagController tagManager))
                return tagManager;

            tagManager = target.GetOrAddComponent<TagController>();
            objectToTagManagerCache.Add(target, tagManager);
            return tagManager;
        }

        public static bool AddTag(this GameObject target, Tag tag, bool dontAllowDuplicates = true)
        {
            TagController tagController = target.GetTagManager();
            if (dontAllowDuplicates && tagController.Contains(tag))
                return false;
            
            tagController.Add(tag);
            return true;
        }

        public static bool RemoveTag(this GameObject target, Tag tag)
        {
            TagController tagController = target.GetTagManager();
            return tagController.Remove(tag);
        }

        public static bool ContainsTag(this GameObject target, Tag tag)
        {
            TagController tagController = target.GetTagManager();
            return tagController.Contains(tag);
        }
    }
}