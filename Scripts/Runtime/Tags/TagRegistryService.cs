using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    [ServiceImplementation]
    public class TagRegistryService
    {
        private sealed class TagSourceEntry
        {
            public ITagSource Source;
            public Component SourceComponent;
            public GameObject GameObject;

            public bool IsAlive => SourceComponent != null;
            public bool IsQueryable => SourceComponent != null && GameObject.activeInHierarchy;
            public IReadOnlyList<Tag> Tags => Source.Tags;
        }

        private const int INITIAL_SIZE = 100;

        private readonly Dictionary<GameObject, TagSourceEntry> _sourceEntries = new(INITIAL_SIZE);

        public void RegisterTagSource(ITagSource tagSource)
        {
            if (tagSource is not Component component)
            {
                Debug.LogError($"[{nameof(TagRegistryService)}] {tagSource?.GetType().Name} must be a Component to be registered");
                return;
            }

            GameObject gameObject = component.gameObject;
            if (_sourceEntries.TryGetValue(gameObject, out TagSourceEntry existing))
            {
                if (existing.IsAlive)
                    return;

                _sourceEntries.Remove(gameObject);
            }

            _sourceEntries.Add(gameObject, new TagSourceEntry
            {
                Source = tagSource,
                SourceComponent = component,
                GameObject = gameObject,
            });
        }

        public void UnregisterTagSource(ITagSource tagSource)
        {
            if (tagSource is not Component component)
                return;

            if (_sourceEntries.TryGetValue(component.gameObject, out TagSourceEntry entry)
                && (ReferenceEquals(entry.Source, tagSource) || !entry.IsAlive))
            {
                _sourceEntries.Remove(component.gameObject);
            }
        }

        public bool TryGetTagsFromGameObject(GameObject targetGameObject, out IReadOnlyList<Tag> tags)
        {
            tags = Array.Empty<Tag>();

            if (targetGameObject == null)
                return false;

            if (!_sourceEntries.TryGetValue(targetGameObject, out TagSourceEntry entry))
                return false;

            if (!entry.IsAlive)
            {
                _sourceEntries.Remove(targetGameObject);
                return false;
            }

            IReadOnlyList<Tag> entryTags = entry.Tags;
            if (entryTags == null || entryTags.Count == 0)
                return false;

            tags = entryTags;
            return true;
        }

        public bool TryGetGameObjectByQueryMatch(CollectionItemQuery<Tag> tagQuery, out GameObject resultGameObj)
        {
            foreach (TagSourceEntry entry in _sourceEntries.Values)
            {
                if (!entry.IsQueryable)
                    continue;

                if (tagQuery.Matches(entry.Tags))
                {
                    resultGameObj = entry.GameObject;
                    return true;
                }
            }

            resultGameObj = null;
            return false;
        }

        public bool TryGetAllGameObjectsByQueryMatch(CollectionItemQuery<Tag> tagQuery, List<GameObject> results)
        {
            int countBefore = results.Count;
            foreach (TagSourceEntry entry in _sourceEntries.Values)
            {
                if (!entry.IsQueryable)
                    continue;

                if (tagQuery.Matches(entry.Tags))
                    results.Add(entry.GameObject);
            }

            return results.Count > countBefore;
        }

        public bool TryGetAllGameObjectsByTag(Tag tag, List<GameObject> results)
        {
            int countBefore = results.Count;
            foreach (TagSourceEntry entry in _sourceEntries.Values)
            {
                if (!entry.IsQueryable)
                    continue;

                if (ContainsTag(entry.Tags, tag))
                    results.Add(entry.GameObject);
            }

            return results.Count > countBefore;
        }

        public bool TryGetFirstGameObjectByTag(Tag tag, out GameObject resultGameObj)
        {
            foreach (TagSourceEntry entry in _sourceEntries.Values)
            {
                if (!entry.IsQueryable)
                    continue;

                if (ContainsTag(entry.Tags, tag))
                {
                    resultGameObj = entry.GameObject;
                    return true;
                }
            }

            resultGameObj = null;
            return false;
        }

        public bool TryGetComponentByTag<T>(Tag targetTag, out T resultComponent) where T : Component
        {
            foreach (TagSourceEntry entry in _sourceEntries.Values)
            {
                if (!entry.IsQueryable)
                    continue;

                if (!ContainsTag(entry.Tags, targetTag))
                    continue;

                if (entry.GameObject.TryGetComponent(out resultComponent))
                    return true;
            }

            resultComponent = null;
            return false;
        }

        private static bool ContainsTag(IReadOnlyList<Tag> tags, Tag tag)
        {
            if (tags == null)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == tag)
                    return true;
            }

            return false;
        }
    }
}
