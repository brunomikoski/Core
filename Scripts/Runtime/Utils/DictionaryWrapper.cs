using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoyTheunissen.Scaffolding.Collections
{
    [Serializable]
    public abstract class DictionaryWrapperPairBase
    {
    }

    [Serializable]
    public sealed class DictionaryWrapperPair<KeyType, ValueType>
        : DictionaryWrapperPairBase
    {
        public KeyType Key;
        public ValueType Value;
    }

    [Serializable]
    public abstract class DictionaryWrapperBase
    {
        public abstract void ClearCache();
    }
    
    // NOTE:    Sometimes the value dictionary needs to be cast before we call its methods.
    //          That is because some methods are implemented explicitly, and you can only
    //          get to them when cast as the interface type. See:
    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/0b72b73d-28bc-476f-9a55-3156fdda4632/where-did-isreadonly-property-go-in-idictionary-and-dictionary?forum=netfxbcl
    [Serializable]
    public sealed class DictionaryWrapper<KeyType, ValueType>
        : DictionaryWrapperBase, IDictionary<KeyType, ValueType>
    {
        [FormerlySerializedAs("values")]
        [SerializeField]
        private List<DictionaryWrapperPair<KeyType, ValueType>> pairs =
            new List<DictionaryWrapperPair<KeyType, ValueType>>();

        public List<DictionaryWrapperPair<KeyType, ValueType>> PairsInOrder => pairs;

        private Dictionary<KeyType, ValueType> cachedDictionary;

        public Dictionary<KeyType, ValueType> GetDictionary()
        {
            if (cachedDictionary != null)
                return cachedDictionary;

            cachedDictionary = new Dictionary<KeyType, ValueType>();
            for (int i = 0; i < pairs.Count; i++)
                cachedDictionary.Add(pairs[i].Key, pairs[i].Value);

            return cachedDictionary;
        }

        public ICollection<KeyType> Keys => GetDictionary().Keys;

        public ICollection<ValueType> Values => GetDictionary().Values;

        public int Count => GetDictionary().Count;

        public bool IsReadOnly => ((IDictionary<KeyType, ValueType>)GetDictionary()).IsReadOnly;

        public ValueType this[KeyType key]
        {
            get { return GetDictionary()[key]; }
            set { GetDictionary()[key] = value; }
        }

        public void Add(KeyType key, ValueType value)
        {
            GetDictionary().Add(key, value);
        }

        public bool ContainsKey(KeyType key)
        {
            return GetDictionary().ContainsKey(key);
        }

        public bool Remove(KeyType key)
        {
            return GetDictionary().Remove(key);
        }

        public bool TryGetValue(KeyType key, out ValueType value)
        {
            return GetDictionary().TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<KeyType, ValueType> item)
        {
            GetDictionary().Add(item.Key, item.Value);
        }

        public void Clear()
        {
            GetDictionary().Clear();
        }

        public bool Contains(KeyValuePair<KeyType, ValueType> item)
        {
            return GetDictionary().ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<KeyType, ValueType>[] array, int arrayIndex)
        {
            // Have to cast first because it was implemented explicitly..
            ((IDictionary<KeyType, ValueType>)GetDictionary()).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<KeyType, ValueType> item)
        {
            return GetDictionary().Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<KeyType, ValueType>> GetEnumerator()
        {
            return GetDictionary().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetDictionary().GetEnumerator();
        }

        public override void ClearCache()
        {
            cachedDictionary = null;
        }
    }
}
