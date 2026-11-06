using System;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections;
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
        private readonly List<Tag> _tagsById = new();
        private readonly Dictionary<Tag, int> _idsByTag = new();

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

        // IDs start at 1, so 0 can mean "no tag". They are sorted by tag GUID, so every client of the same build gets the same IDs.
        public bool TryGetTagId(Tag tag, out int tagId)
        {
            if (tag == null)
            {
                tagId = 0;
                return false;
            }

            BuildTagIdsIfNeeded();
            return _idsByTag.TryGetValue(tag, out tagId);
        }

        public bool TryGetTagById(int tagId, out Tag tag)
        {
            BuildTagIdsIfNeeded();
            if (tagId < 1)
            {
                tag = null;
                return false;
            }

            if (tagId > _tagsById.Count)
            {
                tag = null;
                return false;
            }

            tag = _tagsById[tagId - 1];
            return true;
        }

        private void BuildTagIdsIfNeeded()
        {
            if (_tagsById.Count > 0)
                return;

            List<ScriptableObject> items = CollectionsRegistry.Instance.GetAllCollectionItemsOfType(typeof(Tag));
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not Tag tag)
                    continue;

                _tagsById.Add(tag);
            }

            _tagsById.Sort(CompareByGuid);
            for (int i = 0; i < _tagsById.Count; i++)
                _idsByTag[_tagsById[i]] = i + 1;
        }

        private static int CompareByGuid(Tag first, Tag second)
        {
            (long firstHigh, long firstLow) = first.GUID.GetRawValues();
            (long secondHigh, long secondLow) = second.GUID.GetRawValues();
            int result = firstHigh.CompareTo(secondHigh);
            if (result != 0)
                return result;

            return firstLow.CompareTo(secondLow);
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
