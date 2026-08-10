using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-999)]
    public abstract class TaggeableObjectBase<T> : MonoBehaviour, ITagSource, IList<T> where T : Tag
    {
        [SerializeField]
        private CollectionItemPicker<T> _tags  = new();
        public IReadOnlyList<Tag> Tags => _tags.Items;

        private readonly ServiceReference<TagRegistryService> tagRegistryService = new();

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
            _tags.Add(item);
        }

        public void Clear()
        {
            _tags.Clear();
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
            return _tags.Remove(item);
        }

        public int Count => _tags.Count;
        public bool IsReadOnly => _tags.IsReadOnly;
        public int IndexOf(T item)
        {
            return _tags.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _tags.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _tags.RemoveAt(index);
        }

        public T this[int index]
        {
            get => _tags[index];
            set => _tags[index] = value;
        }
    }
}
