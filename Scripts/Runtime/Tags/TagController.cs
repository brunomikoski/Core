using System;
using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public class TagController : MonoBehaviour, IList<Tag>
    {
        [SerializeField]
        private CollectionItemPicker<Tag> tags = new CollectionItemPicker<Tag>();


        private ServiceReference<TagRegistryService> tagRegistryService= new();

        private void Awake()
        {
            tagRegistryService.Reference.RegisterTagController(this);
        }

        private void OnDestroy()
        {
            if (tagRegistryService.Exists)
                tagRegistryService.CachedReference.UnregisterTagController(this);
        }

        public IEnumerator<Tag> GetEnumerator()
        {
            return tags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Tag item)
        {
            tags.Add(item);
        }

        public void Clear()
        {
            tags.Clear();
        }

        public bool Contains(Tag item)
        {
            return tags.Contains(item);
        }

        public void CopyTo(Tag[] array, int arrayIndex)
        {
            tags.CopyTo(array, arrayIndex);
        }

        public bool Remove(Tag item)
        {
            return tags.Remove(item);
        }

        public int Count => tags.Count;
        public bool IsReadOnly => tags.IsReadOnly;
        public int IndexOf(Tag item)
        {
            return tags.IndexOf(item);
        }

        public void Insert(int index, Tag item)
        {
            tags.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            tags.RemoveAt(index);
        }

        public Tag this[int index]
        {
            get => tags[index];
            set => tags[index] = value;
        }
    }
}