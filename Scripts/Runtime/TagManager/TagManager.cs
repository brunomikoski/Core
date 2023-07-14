using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public class TagManager : MonoBehaviour, IList<TagID>
    {
        [SerializeField]
        private CollectionItemPicker<TagID> tags = new CollectionItemPicker<TagID>();
        
        public IEnumerator<TagID> GetEnumerator()
        {
            return tags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TagID item)
        {
            tags.Add(item);
        }

        public void Clear()
        {
            tags.Clear();
        }

        public bool Contains(TagID item)
        {
            return tags.Contains(item);
        }

        public void CopyTo(TagID[] array, int arrayIndex)
        {
            tags.CopyTo(array, arrayIndex);
        }

        public bool Remove(TagID item)
        {
            return tags.Remove(item);
        }

        public int Count => tags.Count;
        public bool IsReadOnly => tags.IsReadOnly;
        public int IndexOf(TagID item)
        {
            return tags.IndexOf(item);
        }

        public void Insert(int index, TagID item)
        {
            tags.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            tags.RemoveAt(index);
        }

        public TagID this[int index]
        {
            get => tags[index];
            set => tags[index] = value;
        }
    }
}