using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.Utils
{
    /// <summary>
    /// Generic, inspector-serializable loot table that selects items based on their weight.
    /// Works for UnityEngine.Object types, strings, and any other serializable reference type.
    /// </summary>
    [Serializable]
    public class LootTable<T> where T : class
    {
        private static readonly IEqualityComparer<T> ItemComparer = EqualityComparer<T>.Default;

        [Serializable]
        public class Entry
        {
            [SerializeField] public T Item;

            [Min(0f)]
            [SerializeField] public float Weight = 1f;

            public Entry()
            {
            }

            public Entry(T item, float weight)
            {
                Item = item;
                Weight = Mathf.Max(0f, weight);
            }
        }

        [SerializeField]
        private List<Entry> _entries = new List<Entry>();

        [SerializeField]
        private bool _avoidConsecutiveDuplicate;

        [Tooltip("Uses Pseudo-Random Distribution (like Dota 2). Items that haven't been picked " +
                 "get progressively higher odds, preventing long dry streaks.")]
        [SerializeField]
        private bool _pseudoRandom;

        [NonSerialized]
        private T _lastPicked;

        [NonSerialized]
        private int[] _missStreaks;

        [NonSerialized]
        private System.Random _externalRng;

        private static float UnityRandom(float max) => UnityEngine.Random.Range(0f, max);
        private float ExternalRandom(float max) => (float)(_externalRng.NextDouble() * max);

        public List<Entry> Entries => _entries;

        public bool AvoidConsecutiveDuplicate
        {
            get => _avoidConsecutiveDuplicate;
            set => _avoidConsecutiveDuplicate = value;
        }

        public bool PseudoRandom
        {
            get => _pseudoRandom;
            set => _pseudoRandom = value;
        }

        public void ResetConsecutivePickMemory()
        {
            _lastPicked = null;
        }

        public void ResetPseudoRandomState()
        {
            _missStreaks = null;
        }

        public int Count => _entries.Count;

        public float this[T item]
        {
            get
            {
                int index = IndexOf(item);
                return index >= 0 ? _entries[index].Weight : 0f;
            }
            set
            {
                float weight = Mathf.Max(0f, value);
                int index = IndexOf(item);
                if (index >= 0)
                    _entries[index].Weight = weight;
                else
                    _entries.Add(new Entry(item, weight));
            }
        }

        public void Add(T item, float weight = 1f)
        {
            _entries.Add(new Entry(item, Mathf.Max(0f, weight)));
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                _entries.RemoveAt(index);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _entries.Clear();
        }

        private int IndexOf(T item)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (ItemComparer.Equals(_entries[i].Item, item))
                    return i;
            }

            return -1;
        }

        public float GetTotalWeight()
        {
            if (_entries.Count == 0)
                return 0f;

            float total = 0f;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Item == null)
                    continue;

                if (e.Weight > 0f)
                    total += e.Weight;
            }

            return total;
        }

        public T Get()
        {
            T result = _pseudoRandom
                ? PickPseudoRandom(UnityRandom)
                : PickWeighted(UnityRandom);
            RememberPick(result);
            return result;
        }

        public T Get(System.Random rng)
        {
            if (rng == null)
                return Get();

            _externalRng = rng;
            T result = _pseudoRandom
                ? PickPseudoRandom(ExternalRandom)
                : PickWeighted(ExternalRandom);
            _externalRng = null;
            RememberPick(result);
            return result;
        }

        public bool TryGet(out T result)
        {
            result = Get();
            return result != null;
        }

        private void RememberPick(T result)
        {
            if (_avoidConsecutiveDuplicate)
                _lastPicked = result;
        }

        private T PickWeighted(Func<float, float> randomUnderMax)
        {
            T exclude = _avoidConsecutiveDuplicate ? _lastPicked : null;
            float totalExcluding = exclude != null ? GetTotalWeightExcluding(exclude) : 0f;
            bool useExclude = exclude != null && totalExcluding > 0f;
            float total = useExclude ? totalExcluding : GetTotalWeight();
            if (total <= 0f)
                return null;

            float pick = randomUnderMax(total);
            return useExclude ? GetAtWeightExcluding(pick, exclude) : GetAtWeight(pick);
        }

        private float GetTotalWeightExcluding(T exclude)
        {
            if (exclude == null)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Item == null || e.Weight <= 0f || ItemComparer.Equals(e.Item, exclude))
                    continue;
                sum += e.Weight;
            }

            return sum;
        }

        private T GetAtWeight(float target)
        {
            if (_entries.Count == 0)
                return null;

            float cumulative = 0f;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Item == null || e.Weight <= 0f)
                    continue;

                cumulative += e.Weight;
                if (target <= cumulative)
                    return e.Item;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Item != null && e.Weight > 0f)
                    return e.Item;
            }

            return null;
        }

        private T GetAtWeightExcluding(float target, T exclude)
        {
            if (_entries.Count == 0)
                return null;

            float cumulative = 0f;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Item == null || e.Weight <= 0f || ItemComparer.Equals(e.Item, exclude))
                    continue;

                cumulative += e.Weight;
                if (target <= cumulative)
                    return e.Item;
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Item != null && e.Weight > 0f && !ItemComparer.Equals(e.Item, exclude))
                    return e.Item;
            }

            return null;
        }

        private T PickPseudoRandom(Func<float, float> randomUnderMax)
        {
            if (_entries.Count == 0)
                return null;

            if (_missStreaks == null || _missStreaks.Length != _entries.Count)
                _missStreaks = new int[_entries.Count];

            T exclude = _avoidConsecutiveDuplicate ? _lastPicked : null;

            float total = 0f;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Item == null || e.Weight <= 0f)
                    continue;
                if (exclude != null && ItemComparer.Equals(e.Item, exclude))
                    continue;

                total += e.Weight * (1 + _missStreaks[i]);
            }

            if (total <= 0f)
                return null;

            float pick = randomUnderMax(total);
            float cumulative = 0f;
            int pickedIndex = -1;
            T pickedItem = null;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                if (e.Item == null || e.Weight <= 0f)
                    continue;
                if (exclude != null && ItemComparer.Equals(e.Item, exclude))
                    continue;

                cumulative += e.Weight * (1 + _missStreaks[i]);
                if (pick <= cumulative)
                {
                    pickedIndex = i;
                    pickedItem = e.Item;
                    break;
                }
            }

            if (pickedItem == null)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    Entry e = _entries[i];
                    if (e.Item != null && e.Weight > 0f &&
                        (exclude == null || !ItemComparer.Equals(e.Item, exclude)))
                    {
                        pickedIndex = i;
                        pickedItem = e.Item;
                        break;
                    }
                }
            }

            if (pickedIndex >= 0)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (i == pickedIndex)
                        _missStreaks[i] = 0;
                    else if (_entries[i].Item != null && _entries[i].Weight > 0f)
                        _missStreaks[i]++;
                }
            }

            return pickedItem;
        }
    }
}
