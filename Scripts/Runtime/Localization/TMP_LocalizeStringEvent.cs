using System;
using BrunoMikoski.Core.SafeEvents;
using TMPro;
using UnityEngine;

#if UNITY_LOCALIZATION_AVAILABLE
using UnityEngine.Localization.Components;
#endif

namespace BrunoMikoski.Core.Utils.Localization
{
#if UNITY_LOCALIZATION_AVAILABLE
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    public class TMP_LocalizeStringEvent : LocalizeStringEvent
    {
        private bool _hasCachedText;
        private TMP_Text _cachedText;
        public TMP_Text Text
        {
            get
            {
                if (!_hasCachedText)
                {
                    _cachedText = GetComponent<TMP_Text>();
                    _hasCachedText = true;
                }
                return _cachedText;
            }
        }

        private readonly SafeEvent<string> _stringUpdatedEvent = new SafeEvent<string>();
        public event Action<string> StringUpdated
        {
            add => _stringUpdatedEvent.AddListener(value);
            remove => _stringUpdatedEvent.RemoveListener(value);
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            _hasCachedText = false;
        }

 
        protected override void OnDisable()
        {
            base.OnDisable();
#if UNITY_EDITOR
            if (!Application.isPlaying && Text != null)
                UnityEngine.Localization.EditorPropertyDriver.UnregisterProperty(Text, "m_text");
#endif
        }

        protected override void UpdateString(string value)
        {
            base.UpdateString(value);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEngine.Localization.EditorPropertyDriver.RegisterProperty(Text, "m_text");
#endif
            Text.SetText(value);
            _stringUpdatedEvent.Invoke(value);
        }

        public void Clear() {
            StringReference = null;
            Text.SetText(string.Empty);
            OnEnable();
        }

        [ContextMenu("Force Refresh")]
        public void ForceRefresh() {
            RefreshString();
            UpdateString(StringReference.GetLocalizedString(StringReference.Arguments));
        }
        
    }
#endif
}
