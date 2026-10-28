using System;
using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.Core.SafeEvents;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-999)]
    public abstract class TaggeableObjectBase<T> : MonoBehaviour, IMutableTagSource, IList<T> where T : Tag
    {
        [SerializeField]
        private CollectionItemPicker<T> _tags  = new();
        public IReadOnlyList<Tag> Tags => _tags.Items;

        private readonly ServiceReference<TagRegistryService> tagRegistryService = new();

        private readonly SafeEvent<Tag> _tagAdded = new();
        public event Action<Tag> OnTagAddedEvent
        {
            add => _tagAdded.AddListener(value);
            remove => _tagAdded.RemoveListener(value);
        }

        private readonly SafeEvent<Tag> _tagRemoved = new();
        public event Action<Tag> OnTagRemovedEvent
        {
            add => _tagRemoved.AddListener(value);
            remove => _tagRemoved.RemoveListener(value);
        }

        private void Awake()
        {
            Register();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        protected virtual void Register()
        {
            if (!tagRegistryService.Exists)
            {
                throw new System.Exception($"Missing {nameof(TagRegistryService)}. This usually means the scene was played without the services being registered — make sure the scene is set up as a gameplay scene (added to Build Settings with a valid build index) so its services reporter runs.");
            }
            tagRegistryService.Reference.RegisterTagSource(this);
        }

        protected virtual void Unregister()
        {
            if (tagRegistryService.HasCachedReference)
                tagRegistryService.Reference.UnregisterTagSource(this);
        }

        public bool AddTag(Tag tag)
        {
            if (tag is not T typedTag)
            {
                Debug.LogError($"TaggeableObjectBase->AddTag: {tag} is not a {typeof(T).Name}, cannot add it to {name}", this);
                return false;
            }

            if (Contains(typedTag))
                return false;

            Add(typedTag);
            return true;
        }

        public bool RemoveTag(Tag tag)
        {
            if (tag is not T typedTag)
                return false;

            return Remove(typedTag);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (_tags.Contains(item))
                return;

            _tags.Add(item);
            _tagAdded.Invoke(item);
        }

        public void Clear()
        {
            for (int i = _tags.Count - 1; i >= 0; i--)
                RemoveAt(i);
        }

        public bool Contains(T item)
        {
            return _tags.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _tags.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (!_tags.Remove(item))
                return false;

            _tagRemoved.Invoke(item);
            return true;
        }

        public int Count => _tags.Count;
        public bool IsReadOnly => _tags.IsReadOnly;
        public int IndexOf(T item)
        {
            return _tags.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (_tags.Contains(item))
                return;

            _tags.Insert(index, item);
            _tagAdded.Invoke(item);
        }

        public void RemoveAt(int index)
        {
            T item = _tags[index];
            _tags.RemoveAt(index);
            _tagRemoved.Invoke(item);
        }

        public T this[int index]
        {
            get => _tags[index];
            set
            {
                T previous = _tags[index];
                if (previous == value)
                    return;

                _tags[index] = value;
                _tagRemoved.Invoke(previous);
                _tagAdded.Invoke(value);
            }
        }
    }
}
