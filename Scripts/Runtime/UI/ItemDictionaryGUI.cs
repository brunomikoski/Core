using System.Collections.Generic;
#if SIMPLEPOOL_AVAILABLE
using BrunoMikoski.Pooling;
#endif
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.GUIUtils
{
    public class ItemDictionaryGUI<T, TGUI> : MonoBehaviour where TGUI : Component, IItemGUI<T>
    {
        [Header("References")]
        [SerializeField]
        protected TGUI itemTemplate;
        
        [Header("Settings")]
#if SIMPLEPOOL_AVAILABLE
        [SerializeField]
        private bool usePool = true;
        protected bool UsePool => usePool;
#endif
        [SerializeField] 
        protected int initialPoolSize;

        
        public TGUI LastClickedGUI { get; private set; }

        #if USE_ORDERED_DICTIONARY
        protected readonly OrderedDictionary<T, TGUI> items = new();
        public OrderedDictionary<T, TGUI> Items => items;
        #else
        protected readonly Dictionary<T, TGUI> items = new();
        public Dictionary<T, TGUI> Items => items;
        #endif

        private bool isInitialized;

        public int Count => Items.Count;

        public virtual void ShowAll()
        {
            foreach (TGUI item in Items.Values)
            {
                item.gameObject.SetActive(true);
            }
        }

        public virtual void HideAll()
        {
            foreach (TGUI item in Items.Values)
            {
                item.gameObject.SetActive(false);
            }
        }

        public virtual TGUI AddItem(T itemId, bool activated = true)
        {
            Initialize();

            if (items.TryGetValue(itemId, out TGUI resultItem)) {
	            DestroyItem(resultItem);
            }

            TGUI item = SpawnItem(itemId);
            item.Initialize(itemId);

            item.gameObject.SetActive(activated);

            items[itemId] = item;
            return item;
        }

        protected virtual TGUI SpawnItem(T itemId)
        {
            TGUI spawnItem;
#if SIMPLEPOOL_AVAILABLE
            if (usePool)
                spawnItem = SimplePool.Spawn(itemTemplate, itemTemplate.transform.parent);
            else
#endif
                spawnItem = Instantiate(itemTemplate, itemTemplate.transform.parent);
            
            spawnItem.transform.localScale = Vector3.one;
            spawnItem.transform.localPosition = Vector3.zero;
            return spawnItem;
        }

        protected virtual bool RemoveItem(T itemId)
        {
            if (!items.TryGetValue(itemId, out TGUI item))
                return false;

            DestroyItem(item);
            items.Remove(itemId);

            return true;
        }

        public virtual void Populate(IList<T> collection, bool activated = true)
        {
            Clear();
            for (int i = 0; i < collection.Count; i++)
            {
                T itemId = collection[i];
                AddItem(itemId, activated);
            }
        }

        public virtual void Clear()
        {
            DisableTemplate();
            foreach (TGUI item in items.Values)
            {
                DestroyItem(item);
            }

            items.Clear();
        }

        protected virtual void DestroyItem(TGUI item)
        {
#if SIMPLEPOOL_AVAILABLE
            if (usePool)
            {
                if (item != null && item.gameObject != null)
                {
                    SimplePool.Despawn(item.gameObject);
                }
            }
            else
            {
#endif
                Destroy(item.gameObject);
#if SIMPLEPOOL_AVAILABLE
            }
#endif
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
#if SIMPLEPOOL_AVAILABLE
            
            if (!usePool)
                return;

            SimplePool.Preload(itemTemplate, initialPoolSize, gameObject.scene);
#endif
        }

        protected virtual void OnItemClick(TGUI itemGUI)
        {
        }

        protected virtual void OnValidate()
        {
            if (!gameObject.IsInScene())
                return;

            if (itemTemplate == null)
            {
                Debug.LogError("No item template assigned", this);
            }
        }
        
        protected virtual void OnDestroy()
        {
            Clear();
#if SIMPLEPOOL_AVAILABLE
            if (usePool)
            {
                SimplePool.DestroyPool(itemTemplate);
            }
#endif
        }
    }
}
