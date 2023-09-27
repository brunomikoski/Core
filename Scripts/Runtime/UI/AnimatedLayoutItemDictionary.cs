using System.Collections.Generic;
using BrunoMikoski.Pooling;
using UnityEngine;

namespace BrunoMikoski.Core.GUIUtils
{
    public class AnimatedLayoutItemDictionary<TItemIDType, TItemGUI> : ItemDictionaryGUI<TItemIDType, TItemGUI>
        where TItemGUI : Component, IItemGUI<TItemIDType>
    {
        [Header("References")] 
        [SerializeField]
        private RectTransform layoutElementTemplate;

        [Header("Settings")] 
        [SerializeField]
        private float moveSpeed;

        private Dictionary<TItemGUI, RectTransform> itemToLayoutItem = new();


        public override void Populate(IList<TItemIDType> collection, bool activated = true)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                TItemIDType itemIDType = collection[i];
                if (items.ContainsKey(itemIDType))
                    continue;

                AddItem(itemIDType, activated);
            }
            
            foreach (var item in items)
            {
                if (collection.Contains(item.Key))
                    continue;

                RemoveItem(item.Key);
            }
        }

        protected override void DisableTemplate()
        {
            base.DisableTemplate();
            layoutElementTemplate.gameObject.SetActive(false);
        }

        protected override void CreatePools()
        {
            base.CreatePools();
            if (UsePool)
                SimplePool.Preload(layoutElementTemplate, initialPoolSize);
        }

        public override TItemGUI AddItem(TItemIDType itemId, bool activated = true)
        {
            TItemGUI baseItem = base.AddItem(itemId, activated);

            if (itemToLayoutItem.TryGetValue(baseItem, out RectTransform layoutElement))
            {
                DestroyRectElement(layoutElement);
                itemToLayoutItem.Remove(baseItem);
            }
            
            layoutElement = SpawnLayoutElement();
            layoutElement.gameObject.SetActive(activated);
            
            itemToLayoutItem.Add(baseItem, layoutElement);
            return baseItem;
        }

        protected override bool RemoveItem(TItemIDType itemId)
        {
            bool removed =  base.RemoveItem(itemId);
            if (!removed)
                return false;

            if (itemToLayoutItem.TryGetValue(items[itemId], out RectTransform layoutElement))
            {
                DestroyRectElement(layoutElement);
                itemToLayoutItem.Remove(items[itemId]);
            }

            return true;
        }

        private void DestroyRectElement(RectTransform layoutElement)
        {
            if (UsePool)
                SimplePool.Despawn(layoutElement.gameObject);
            else
                Destroy(layoutElement.gameObject);
        }

        private RectTransform SpawnLayoutElement()
        {
            RectTransform item;
            if (UsePool)
                item = SimplePool.Spawn(layoutElementTemplate, layoutElementTemplate.transform.parent);
            else
                item = Instantiate(layoutElementTemplate, layoutElementTemplate.transform.parent);

            item.transform.localScale = Vector3.one;
            item.transform.localPosition = Vector3.zero;
            return item;
        }

        private void Update()
        {
            MoveObjects();
        }

        private void MoveObjects()
        {
            foreach (var itemToLayout in itemToLayoutItem)
            {
                itemToLayout.Key.transform.position = Vector3.Lerp(itemToLayout.Key.transform.position, itemToLayout.Value.position, Time.deltaTime * moveSpeed);
            }
        }
    }
}
