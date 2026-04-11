using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core
{
    [ServiceImplementation]
    public class TagRegistryService
    {
        private const int INITIAL_SIZE = 100;

        private readonly Dictionary<GameObject, IReadOnlyList<Tag>> _gameObjectToTagsCache = new(INITIAL_SIZE);


        private readonly Dictionary<Tag, List<GameObject>> _tagToGameObjectsCache = new();
        private readonly Dictionary<(Tag tag, System.Type type), Component> _tagToComponentsCache = new();

        public void RegisterTagController(ITagSource tagController)
        {
            if (tagController is Component component)
            {
                RegisterObjectToTags(component.gameObject, tagController.Tags);
            }
        }

        public void UnregisterTagController(ITagSource tagController)
        {
            if (tagController is Component component)
            {
                UnregisterObjectToTags(component.gameObject);
            }
        }


        public bool TryGetGameObjectByQueryMatch(CollectionItemQuery<Tag> tagQuery, out GameObject resultGameObj)
        {
            foreach (KeyValuePair<GameObject, IReadOnlyList<Tag>> entry in _gameObjectToTagsCache)
            {
                if (entry.Key == null)
                {
                    continue;
                }

                if (tagQuery.Matches(entry.Value))
                {
                    resultGameObj = entry.Key;
                    return resultGameObj != null;
                }
            }

            resultGameObj = null;
            return false;
        }

        public bool TryGetAllGameObjectsByTag(Tag tag, out List<GameObject> resultGameObjs)
        {
            if (_tagToGameObjectsCache.TryGetValue(tag, out resultGameObjs))
            {
                if (resultGameObjs.Count > 0)
                {
                    return true;
                }
            }
            else
            {
                resultGameObjs = new List<GameObject>();
                _tagToGameObjectsCache.Add(tag, resultGameObjs);
            }

            foreach (KeyValuePair<GameObject, IReadOnlyList<Tag>> entry in _gameObjectToTagsCache)
            {
                GameObject gameObject = entry.Key;

                if (gameObject == null)
                {
                    continue;
                }

                if (entry.Value != null && entry.Value.Contains(tag))
                {
                    resultGameObjs.Add(gameObject);
                }
            }

            return resultGameObjs.Count > 0;
        }

        public bool TryGetFirstGameObjectByTag(Tag tag, out GameObject resultGameObj)
        {
            if (TryGetAllGameObjectsByTag(tag, out List<GameObject> gameObjects))
            {
                resultGameObj = gameObjects[0];
                return true;
            }

            resultGameObj = null;
            return false;
        }

        public bool TryGetComponentByTag<T>(Tag targetTag, out T resultComponent) where T : Component
        {
            if (_tagToComponentsCache.TryGetValue((targetTag, typeof(T)), out Component component))
            {
                resultComponent = (T)component;
                if (resultComponent != null)
                {
                    return true;
                }
                _tagToComponentsCache.Remove((targetTag, typeof(T)));
            }

            if (TryGetFirstGameObjectByTag(targetTag, out GameObject resultGameObj))
            {
                resultComponent = resultGameObj.GetComponent<T>();
                if (resultComponent != null)
                {
                    _tagToComponentsCache.Add((targetTag, typeof(T)), resultComponent);
                    return true;
                }

                return false;
            }

            resultComponent = null;
            return false;
        }

        public void RegisterObjectToTags(GameObject gameObject, IReadOnlyList<Tag> targetTag)
        {
            _gameObjectToTagsCache.TryAdd(gameObject, targetTag);
        }

        public void UnregisterObjectToTags(GameObject gameObject)
        {
            _gameObjectToTagsCache.Remove(gameObject);

            foreach (KeyValuePair<Tag, List<GameObject>> pair in _tagToGameObjectsCache)
            {
                pair.Value.Remove(gameObject);
            }

            List<Tag> emptyKeys = new List<Tag>();
            foreach (KeyValuePair<Tag, List<GameObject>> pair in _tagToGameObjectsCache)
            {
                if (pair.Value.Count == 0)
                    emptyKeys.Add(pair.Key);
            }
            for (int i = 0; i < emptyKeys.Count; i++)
            {
                _tagToGameObjectsCache.Remove(emptyKeys[i]);
            }

            List<(Tag tag, System.Type type)> componentCacheKeysToRemove = new();
            foreach (KeyValuePair<(Tag tag, System.Type type), Component> kvp in _tagToComponentsCache)
            {
                if (kvp.Value != null && kvp.Value.gameObject == gameObject)
                {
                    componentCacheKeysToRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < componentCacheKeysToRemove.Count; i++)
            {
                _tagToComponentsCache.Remove(componentCacheKeysToRemove[i]);
            }
        }
    }
}
