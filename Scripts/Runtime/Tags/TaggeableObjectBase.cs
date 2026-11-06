using System;
using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.Core.SafeEvents;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-999)]
    public abstract class TaggeableObjectBase<T> : MonoBehaviour, IMutableTagSource, IList<T> where T : Tag
    {
        private struct TimedTag
        {
            public T Tag;
            public T ExpireTag;
            public float EndTime;
        }

        [SerializeField]
        private CollectionItemPicker<T> _tags  = new();
        public IReadOnlyList<Tag> Tags => _tags.Items;

        private readonly ServiceReference<TagRegistryService> tagRegistryService = new();
        private readonly ServiceReference<MonoBehaviourService> monoBehaviourService = new();

        private readonly List<TimedTag> timedTags = new();
        private ITagRelay relay;

        public bool IsReady
        {
            get
            {
                if (relay == null)
                    return true;

                return relay.IsReady;
            }
        }

        private readonly SafeEvent<Tag> _tagAdded = new();
        public event Action<Tag> OnTagAddedEvent
        {
            add => _tagAdded.AddListener(value);
            remove => _tagAdded.RemoveListener(value);
        }

        private readonly SafeEvent<Tag> _tagRemoved = new();
        public event Action<Tag> OnTagRemovedEvent
        {
            add => _tagRemoved.AddListener(value);
            remove => _tagRemoved.RemoveListener(value);
        }

        private readonly SafeEvent _ready = new();
        public event Action OnReadyEvent
        {
            add => _ready.AddListener(value);
            remove => _ready.RemoveListener(value);
        }

        private void Awake()
        {
            Register();
        }

        private void OnDestroy()
        {
            Unregister();

            if (monoBehaviourService.HasCachedReference)
                monoBehaviourService.Reference.UnsubscribeFromUpdate(UpdateTimedTags);

            if (relay != null)
                relay.OnReadyEvent -= OnRelayReady;
        }

        protected virtual void Register()
        {
            if (!tagRegistryService.Exists)
            {
                throw new System.Exception($"Missing {nameof(TagRegistryService)}. This usually means the scene was played without the services being registered — make sure the scene is set up as a gameplay scene (added to Build Settings with a valid build index) so its services reporter runs.");
            }
            tagRegistryService.Reference.RegisterTagSource(this);
        }

        protected virtual void Unregister()
        {
            if (tagRegistryService.HasCachedReference)
                tagRegistryService.Reference.UnregisterTagSource(this);
        }

        public void SetRelay(ITagRelay tagRelay)
        {
            relay = tagRelay;
            relay.OnReadyEvent += OnRelayReady;
        }

        private void OnRelayReady()
        {
            _ready.Invoke();
        }

        public bool AddTag(Tag tag)
        {
            if (tag is not T typedTag)
            {
                Debug.LogError($"TaggeableObjectBase->AddTag: {tag} is not a {typeof(T).Name}, cannot add it to {name}", this);
                return false;
            }

            if (Contains(typedTag))
                return false;

            Add(typedTag);
            return true;
        }

        public bool AddTag(Tag tag, float duration, Tag expireTag = null, TagRefreshMode refreshMode = TagRefreshMode.Restart)
        {
            if (relay != null && relay.CanRelay(tag))
                return relay.RequestAddTag(tag, duration, expireTag, refreshMode);

            if (tag is not T typedTag)
            {
                Debug.LogError($"TaggeableObjectBase->AddTag: {tag} is not a {typeof(T).Name}, cannot add it to {name}", this);
                return false;
            }

            T typedExpireTag = null;
            if (expireTag != null)
            {
                if (expireTag is not T castExpireTag)
                {
                    Debug.LogError($"TaggeableObjectBase->AddTag: expire tag {expireTag} is not a {typeof(T).Name}, cannot add it to {name}", this);
                    return false;
                }

                typedExpireTag = castExpireTag;
            }

            if (ShouldKeepExistingTag(typedTag, refreshMode))
                return false;

            ClearTimedTag(typedTag);
            bool added = AddTag(typedTag);
            if (duration > 0f)
                ScheduleTimedTag(typedTag, duration, typedExpireTag);

            return added;
        }

        public bool RemoveTag(Tag tag)
        {
            if (tag is not T typedTag)
                return false;

            return Remove(typedTag);
        }

        public bool TryGetRemainingTime(Tag tag, out float remainingSeconds)
        {
            if (relay != null && relay.CanRelay(tag))
                return relay.TryGetRemainingTime(tag, out remainingSeconds);

            for (int i = 0; i < timedTags.Count; i++)
            {
                if (timedTags[i].Tag != tag)
                    continue;

                remainingSeconds = Mathf.Max(0f, timedTags[i].EndTime - Time.time);
                return true;
            }

            remainingSeconds = 0f;
            return false;
        }

        private bool ShouldKeepExistingTag(T tag, TagRefreshMode refreshMode)
        {
            switch (refreshMode)
            {
                case TagRefreshMode.KeepExisting:
                    return Contains(tag);
                default:
                    return false;
            }
        }

        private void ScheduleTimedTag(T tag, float duration, T expireTag)
        {
            timedTags.Add(new TimedTag
            {
                Tag = tag,
                ExpireTag = expireTag,
                EndTime = Time.time + duration,
            });

            if (timedTags.Count == 1)
                monoBehaviourService.Reference.SubscribeToUpdate(UpdateTimedTags, MonoBehaviourService.UpdateType.Update);
        }

        private void ClearTimedTag(T tag)
        {
            for (int i = 0; i < timedTags.Count; i++)
            {
                if (timedTags[i].Tag != tag)
                    continue;

                RemoveTimedTagAt(i);
                return;
            }
        }

        private void RemoveTimedTagAt(int index)
        {
            timedTags.RemoveAt(index);
            if (timedTags.Count > 0)
                return;

            monoBehaviourService.Reference.UnsubscribeFromUpdate(UpdateTimedTags);
        }

        private void UpdateTimedTags(float interval)
        {
            while (TryTakeExpiredTimedTag(out TimedTag expiredTag))
            {
                Remove(expiredTag.Tag);
                if (expiredTag.ExpireTag != null)
                    Add(expiredTag.ExpireTag);
            }
        }

        // Removing a tag raises events that can add or remove other timed tags, so take one entry at a time.
        private bool TryTakeExpiredTimedTag(out TimedTag expiredTag)
        {
            for (int i = 0; i < timedTags.Count; i++)
            {
                if (Time.time < timedTags[i].EndTime)
                    continue;

                expiredTag = timedTags[i];
                RemoveTimedTagAt(i);
                return true;
            }

            expiredTag = default;
            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _tags.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (_tags.Contains(item))
                return;

            _tags.Add(item);
            _tagAdded.Invoke(item);
        }

        public void Clear()
        {
            for (int i = _tags.Count - 1; i >= 0; i--)
                RemoveAt(i);
        }

        public bool Contains(T item)
        {
            return _tags.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _tags.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            if (!_tags.Remove(item))
                return false;

            ClearTimedTag(item);
            _tagRemoved.Invoke(item);
            return true;
        }

        public int Count => _tags.Count;
        public bool IsReadOnly => _tags.IsReadOnly;
        public int IndexOf(T item)
        {
            return _tags.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (_tags.Contains(item))
                return;

            _tags.Insert(index, item);
            _tagAdded.Invoke(item);
        }

        public void RemoveAt(int index)
        {
            T item = _tags[index];
            _tags.RemoveAt(index);
            ClearTimedTag(item);
            _tagRemoved.Invoke(item);
        }

        public T this[int index]
        {
            get => _tags[index];
            set
            {
                T previous = _tags[index];
                if (previous == value)
                    return;

                _tags[index] = value;
                ClearTimedTag(previous);
                _tagRemoved.Invoke(previous);
                _tagAdded.Invoke(value);
            }
        }
    }
}
