using System;
using BrunoMikoski.Core.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrunoMikoski.Core.DevelopmentTools.UI
{
    /// <summary>
    /// Reports the layout size of its child, but with a maximum width/height applied.
    /// <example>https://discussions.unity.com/t/psa-max-width-workaround-for-layout-in-ugui/169084
    /// </example>
    /// </summary>
    [ExecuteInEditMode]
    public sealed class LayoutMaxSize : UIBehaviour, ILayoutElement
    {
        [SerializeField] private bool _enforceMaxWidth;
        [ShowIf(nameof(_enforceMaxWidth))]
        [SerializeField] private float _maxWidth;
        
        [SerializeField] private bool _enforceMaxHeight;
        [ShowIf(nameof(_enforceMaxHeight))]
        [SerializeField] private float _maxHeight;
        
        [SerializeField] private int _layoutPriority = 1;
        
        [NonSerialized] private RectTransform cachedRectTransform;
        [NonSerialized] private bool didCacheRectTransform;
        private RectTransform RectTransform
        {
            get
            {
                if (!didCacheRectTransform)
                {
                    didCacheRectTransform = true;
                    cachedRectTransform = GetComponent<RectTransform>();
                }
                return cachedRectTransform;
            }
        }

        private float m_MinWidth = -1;
        private float m_MinHeight = -1;
        private float m_PreferredWidth = -1;
        private float m_PreferredHeight = -1;
        private float m_FlexibleWidth = -1;
        private float m_FlexibleHeight = -1;
        
        private float m_LastKnownChildMinWidth = -1;
        private float m_LastKnownChildMinHeight = -1;
        private float m_LastKnownChildPreferredWidth = -1;
        private float m_LastKnownChildPreferredHeight = -1;
        private float m_LastKnownChildFlexibleWidth = -1;
        private float m_LastKnownChildFlexibleHeight = -1;
        
        public float minWidth => m_MinWidth;
        public float minHeight => m_MinHeight;
        public float preferredWidth => m_PreferredWidth;
        public float preferredHeight => m_PreferredHeight;

        public float flexibleWidth => m_FlexibleWidth;
        public float flexibleHeight => m_FlexibleHeight;

        public float maxWidth
        {
            get
            {
                if (_enforceMaxWidth)
                    return _maxWidth;

                return -1;
            }
        }

        public float maxHeight
        {
            get
            {
                if (_enforceMaxHeight)
                    return _maxHeight;

                return -1;
            }
        }

        public int layoutPriority => _layoutPriority;

        private bool HasChild => transform.childCount > 0;
        private RectTransform Child => transform.GetChild(0) as RectTransform;

        public void CalculateLayoutInputHorizontal()
        {
            if (!HasChild)
            {
                m_MinWidth = -1;
                m_PreferredWidth = -1;
                m_FlexibleWidth = -1;
                return;
            }

            m_LastKnownChildMinWidth = LayoutUtility.GetMinWidth(Child);
            m_LastKnownChildPreferredWidth = LayoutUtility.GetPreferredWidth(Child);
            m_LastKnownChildFlexibleWidth = LayoutUtility.GetFlexibleWidth(Child);
            
            m_MinWidth = GetClampedWidth(m_LastKnownChildMinWidth);
            m_PreferredWidth = GetClampedWidth(m_LastKnownChildPreferredWidth);
            m_FlexibleWidth = m_LastKnownChildFlexibleWidth;
        }

        public void CalculateLayoutInputVertical()
        {
            if (!HasChild)
            {
                m_MinHeight = -1;
                m_PreferredHeight = -1;
                m_FlexibleHeight = -1;
                return;
            }

            m_LastKnownChildMinHeight = LayoutUtility.GetMinHeight(Child);
            m_LastKnownChildPreferredHeight = LayoutUtility.GetPreferredHeight(Child);
            m_LastKnownChildFlexibleHeight = LayoutUtility.GetFlexibleHeight(Child);
            
            m_MinHeight = GetClampedHeight(m_LastKnownChildMinHeight);
            m_PreferredHeight = GetClampedHeight(m_LastKnownChildPreferredHeight);
            m_FlexibleHeight = m_LastKnownChildFlexibleHeight;
        }

        private float GetClampedWidth(float width)
        {
            return _enforceMaxWidth ? Mathf.Min(width, _maxWidth) : width;
        }

        private float GetClampedHeight(float height)
        {
            return _enforceMaxHeight ? Mathf.Min(height, _maxHeight) : height;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            // This is taken from ContentSizeFitter
            SetDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            // This is taken from ContentSizeFitter
            SetDirty(true);
        }

        private void Update()
        {
            // Our layout size depends on that of our child, so if it changes, we need to mark ourselves as dirty.
            // This seems to be the only way to go about it.
            if (HasChildChanged())
                SetDirty();
        }

        private bool HasChildChanged()
        {
            if (!HasChild)
                return false;
            
            if (!Mathf.Approximately(m_LastKnownChildMinWidth, LayoutUtility.GetMinWidth(Child)))
                return true;
            
            if (!Mathf.Approximately(m_LastKnownChildPreferredWidth, LayoutUtility.GetPreferredWidth(Child)))
                return true;
            
            if (!Mathf.Approximately(m_LastKnownChildFlexibleWidth, LayoutUtility.GetFlexibleWidth(Child)))
                return true;

            if (!Mathf.Approximately(m_LastKnownChildMinHeight, LayoutUtility.GetMinHeight(Child)))
                return true;
            
            if (!Mathf.Approximately(m_LastKnownChildPreferredHeight, LayoutUtility.GetPreferredHeight(Child)))
                return true;
            
            if (!Mathf.Approximately(m_LastKnownChildFlexibleHeight, LayoutUtility.GetFlexibleHeight(Child)))
                return true;

            return false;
        }

        // I was hoping that this method gets called whenever a child layout element changes. That does not seem to be
        // the case. It seems to work that way for ContentSizeFitter though, so that probably has to do with it
        // inheriting from ILayoutSelfController or something like that. We're not looking to implement that interface
        // because we're not here to control a RectTransform, we simply report our child's layout properties and clamp
        // the size as needed.
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
        }

        private void SetDirty(bool force = false)
        {
            if (!force && !IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            SetDirty();
        }
#endif
    }
}
