using System.Collections.Generic;
using BrunoMikoski.Pooling;
using UnityEngine;

namespace BrunoMikoski.Core.GUIUtils
{
    public class ItemDictionaryGUI<TItemIDType, TItemGUI> : MonoBehaviour
        where TItemGUI : Component, IItemGUI<TItemIDType>
    {
        [Header("References")]
        [SerializeField]
        protected TItemGUI itemTemplate;
        
        [Header("Settings")]
        [SerializeField]
        private bool usePool = true;
        protected bool UsePool => usePool;
        [SerializeField] 
        protected int initialPoolSize;

        
        public TItemGUI LastClickedGUI { get; private set; }

        protected readonly Dictionary<TItemIDType, TItemGUI> items = new();

        private bool isInitialized;

        public Dictionary<TItemIDType, TItemGUI> Items => items;

        public int Count => Items.Count;

        public virtual void ShowAll()
        {
            foreach (TItemGUI item in Items.Values)
            {
                item.gameObject.SetActive(true);
            }
        }

        public virtual void HideAll()
        {
            foreach (TItemGUI item in Items.Values)
            {
                item.gameObject.SetActive(false);
            }
        }

        public virtual TItemGUI AddItem(TItemIDType itemId, bool activated = true)
        {
            Initialize();

            if (items.TryGetValue(itemId, out TItemGUI resultItem)) {
	            DestroyItem(resultItem);
            }

            TItemGUI item = SpawnItem(itemId);
            item.Initialize(itemId);

            item.gameObject.SetActive(activated);

            items[itemId] = item;
            return item;
        }

        protected virtual TItemGUI SpawnItem(TItemIDType itemId)
        {
            TItemGUI spawnItem;
            if (usePool)
                spawnItem = SimplePool.Spawn(itemTemplate, itemTemplate.transform.parent);
            else
                spawnItem = Instantiate(itemTemplate, itemTemplate.transform.parent);
            
            spawnItem.transform.localScale = Vector3.one;
            spawnItem.transform.localPosition = Vector3.zero;
            return spawnItem;
        }

        protected virtual bool RemoveItem(TItemIDType itemId)
        {
            if (!items.ContainsKey(itemId))
                return false;

            DestroyItem(items[itemId]);
            items.Remove(itemId);

            return true;
        }

        public virtual void Populate(IList<TItemIDType> collection, bool activated = true)
        {
            Clear();
            for (int i = 0; i < collection.Count; i++)
            {
                TItemIDType itemId = collection[i];
                AddItem(itemId, activated);
            }
        }

        public virtual void Clear()
        {
            DisableTemplate();
            foreach (TItemGUI item in items.Values)
            {
                DestroyItem(item);
            }

            items.Clear();
        }

        private void DestroyItem(TItemGUI item)
        {
            if (usePool)
            {
                SimplePool.Despawn(item.gameObject);
            }
            else
            {
                Destroy(item.gameObject);
            }
        }

        protected virtual void DisableTemplate()
        {
            itemTemplate.gameObject.SetActive(false);
        }

        protected virtual void Initialize()
        {
            if (isInitialized)
                return;

            DisableTemplate();
            CreatePools();

            isInitialized = true;
        }

        protected virtual void CreatePools()
        {
            if (!usePool)
                return;

            SimplePool.Preload(itemTemplate, initialPoolSize);
        }

        protected virtual void OnItemClick(TItemGUI itemGUI)
        {
        }

        protected virtual void OnValidate()
        {
            if (itemTemplate == null)
            {
                Debug.LogError("No item template assigned", this);
            }
        }
    }
}