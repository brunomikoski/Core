using System.Collections.Generic;
using BrunoMikoski.Core.CustomEasing;
using BrunoMikoski.Pooling;
using UnityEngine;

namespace BrunoMikoski.Core.GUIUtils
{
    public class AnimatedLayoutItemDictionary<T, TGUI> : ItemDictionaryGUI<T, TGUI> where TGUI : Component, IItemGUI<T>
    {
        [Header("References")] 
        [SerializeField]
        private RectTransform layoutElementTemplate;

        [Header("Settings")] 
        [SerializeField]
        private float moveSpeed;

        private Dictionary<TGUI, RectTransform> itemToLayoutItem = new();
        private HashSet<TGUI> staticItems = new();

        public override void Populate(IList<T> collection, bool activated = true)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                T itemIDType = collection[i];
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

        public override TGUI AddItem(T itemId, bool activated = true)
        {
            TGUI baseItem = base.AddItem(itemId, activated);

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

        protected override bool RemoveItem(T itemId)
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

        public void SetAnimationEnabled(T itemId, bool isEnabled)
        {
            if (!Items.ContainsKey(itemId)) 
                return;

            if (isEnabled)
                staticItems.Remove(Items[itemId]);
            else 
                staticItems.Add(Items[itemId]);
                
        }

        private void Update()
        {
            MoveObjects();
        }

        private void MoveObjects()
        {
            foreach (var itemToLayout in itemToLayoutItem)
            {
                if (staticItems.Contains(itemToLayout.Key))
                    continue;
                
                itemToLayout.Key.transform.position = Vector3.Lerp(itemToLayout.Key.transform.position, itemToLayout.Value.position,
                    Easing.Elastic.Out(Time.deltaTime * moveSpeed));
            }
        }
    }
}
