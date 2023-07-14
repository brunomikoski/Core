using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public static class TagSystemExtensions
    {
        private static Dictionary<object, TagManager> objectToTagManagerCache = new Dictionary<object, TagManager>();

        public static TagManager GetTagManager(this Component target)
        {
            return GetTagManager(target.gameObject);
        }

        public static TagManager GetTagManager(this GameObject target)
        {
            if (objectToTagManagerCache.TryGetValue(target, out TagManager tagManager))
                return tagManager;

            tagManager = target.GetOrAddComponent<TagManager>();
            objectToTagManagerCache.Add(target, tagManager);
            return tagManager;
        }

        public static bool AddTag(this GameObject target, TagID tag, bool dontAllowDuplicates = true)
        {
            TagManager tagManager = target.GetTagManager();
            if (dontAllowDuplicates && tagManager.Contains(tag))
                return false;
            
            tagManager.Add(tag);
            return true;
        }

        public static bool RemoveTag(this GameObject target, TagID tag)
        {
            TagManager tagManager = target.GetTagManager();
            return tagManager.Remove(tag);
        }

        public static bool ContainsTag(this GameObject target, TagID tag)
        {
            TagManager tagManager = target.GetTagManager();
            return tagManager.Contains(tag);
        }
    }
}