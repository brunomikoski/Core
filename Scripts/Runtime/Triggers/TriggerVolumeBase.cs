using System;
using System.Collections.Generic;
using BrunoMikoski.Core.Attributes;
using BrunoMikoski.Core.DevelopmentTools.PhysX;
using BrunoMikoski.Core.SafeEvents;
using BrunoMikoski.Core.Utils;
using BrunoMikoski.ScriptableObjectCollections.Picker;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
     public abstract class TriggerVolumeBase : Trigger
    {
        private const string TRIGGER_HELPER_NAME = "TriggerHelper";

        [Header("Trigger Volume")]
        [SerializeField]
        protected Color gizmoColor = new Color(0, 1, 1, 0.3f);
        [SerializeField] 
        private bool debug;

        [SerializeField]
        private bool allowKinematics = true;

        [SerializeField]
        [Tooltip("Only adds objects to the volume if they are below a certain velocity")]
        private bool triggerIfBelowVelocity;

        [SerializeField, HideIf(nameof(triggerIfBelowVelocity), false)]
        private float velocityThreshold = 2.0f;

        [SerializeField, HideIf(nameof(triggerIfBelowVelocity), false)]
        private float angularVelocityThreshold = 2.0f;

        [SerializeField]
        private LayerMask validLayers = ~0;

        [SerializeField]
        private bool disableFilterRecheck;

        [SerializeField]
        private bool includeOtherTriggerColliders;

        [SerializeField]
        protected bool overrideTriggerLayer;

        [SerializeField, HideIf(nameof(overrideTriggerLayer), false)]
        protected LayerMask triggerLayer;

        [SerializeField]
        private bool useTagFiltering;
        [SerializeField, ShowIf(nameof(useTagFiltering))]
        private CollectionItemQuery<Tag> tagFilter = new();

        protected GameObject triggerChild;
        private Collider triggerCollider;
        public Collider TriggerCollider
        {
            get
            {
                CheckTriggerCollider();
                return triggerCollider;
            }
        }

        protected float Scale
        {
            get
            {
                Vector3 localScale = transform.localScale;
                return MathUtility.Max(localScale.x, localScale.y, localScale.z);
            }
        }

        /// <summary>
        /// Contains all physics objects inside trigger field
        /// </summary>
        private List<ObjectInTriggerVolumeData> objectsInTrigger = new();

        public List<ObjectInTriggerVolumeData> ObjectsInTrigger => objectsInTrigger;

        /// <summary>
        /// Contains all physics objects inside the volume
        /// </summary>
        private List<Rigidbody> objectsInVolume = new();

        public List<Rigidbody> ObjectsInVolume => objectsInVolume;

        //Used for quick lookup if the objects is already in the volume list (list.contains is to slow for every frame)
        private readonly HashSet<Rigidbody> objectsInVolumeSet = new();

        private readonly List<Transform> ignoredObjects = new();

        private ColliderFilterDelegate colliderFilter;
        public ColliderFilterDelegate ColliderFilter => colliderFilter;

        public delegate bool ColliderFilterDelegate(Collider collider);

        private readonly SafeEvent<Rigidbody> onObjectEnterVolume = new();
        public event Action<Rigidbody> OnObjectEnterVolumeEvent
        {
            add => onObjectEnterVolume.AddListener(value);
            remove => onObjectEnterVolume.RemoveListener(value);
        }

        private readonly SafeEvent<Rigidbody> onObjectExitVolume = new();
        public event Action<Rigidbody> OnObjectExitVolumeEvent
        {
            add => onObjectExitVolume.AddListener(value);
            remove => onObjectExitVolume.RemoveListener(value);
        }
        
        private readonly ServiceReference<TagRegistryService> _tagRegistryService = new();

        /// <summary>
        /// Careful when using this event! This event is dispatched after Unity has registered it
        /// as being destroyed, but the RigidbodyWrapper is still accessible. You can use it for cleanup
        /// as long as you don't require MonoBehaviour
        /// </summary>
        private readonly SafeEvent<Rigidbody> onObjectDestroyedInVolume = new();
        public event Action<Rigidbody> OnObjectDestroyedInVolumeEvent
        {
            add => onObjectDestroyedInVolume.AddListener(value);
            remove => onObjectDestroyedInVolume.RemoveListener(value);
        }

        protected virtual void Awake()
        {
            CheckTriggerCollider();
        }

        protected virtual void OnEnable()
        {
            if (TriggerCollider != null)
                TriggerCollider.enabled = true;
        }

        protected void CheckTriggerCollider()
        {
            if (triggerCollider != null)
                return;

            CreateTriggerCollider();
        }

        protected virtual void CreateTriggerCollider()
        {

        }

        protected virtual void Update()
        {
            if (objectsInTrigger.Count == 0 && objectsInVolume.Count == 0)
                return;

            RecheckFilter();
        }

        protected virtual void OnDisable()
        {
            TriggerCollider.enabled = false;
            ClearAllObjectsInVolume();
        }

        private bool ShouldIgnore(Transform transform)
        {
            for (int i = ignoredObjects.Count - 1; i >= 0; i--)
            {
                if (ignoredObjects[i] == null)
                {
                    ignoredObjects.RemoveAt(i);
                    continue;
                }

                if (transform.IsChildOf(ignoredObjects[i]))
                    return true;
            }
            return false;
        }

        private void RecheckFilter()
        {
            int objectsInTriggerAtStart = objectsInTrigger.Count;

            for (int i = objectsInTrigger.Count - 1; i >= 0; i--)
            {
                if (objectsInTriggerAtStart != objectsInTrigger.Count)
                {
                    RecheckFilter();
                    return;
                }

                Rigidbody insideRigidbody = objectsInTrigger[i];
                if (insideRigidbody == null) 
                {
                    objectsInTrigger[i].Dispose();
                    objectsInTrigger.RemoveAt(i);
                    continue;
                }

                if (!disableFilterRecheck)
                {
                    if (!objectsInVolumeSet.Contains(insideRigidbody)
                        && CheckConditions(insideRigidbody))
                    {
                        EnterVolume(insideRigidbody);
                    }
                    else if (objectsInVolume.Contains(insideRigidbody) && !CheckConditions(insideRigidbody))
                    {
                        ExitVolume(insideRigidbody);
                    }
                }

                List<Collider> colliders = objectsInTrigger[i].collidersInVolume;
                for (int j = colliders.Count - 1; j >= 0; j--)
                {
                    if (colliders[j].enabled && colliders[j].gameObject.activeInHierarchy)
                        continue;

                    OnColliderExit(colliders[j], insideRigidbody);
                }
            }

            for (int i = objectsInVolume.Count - 1; i >= 0; i--)
            {
                Rigidbody wrapper = objectsInVolume[i];
                if (wrapper == null)
                {
                    onObjectDestroyedInVolume.Invoke(wrapper);
                    objectsInVolume.RemoveAt(i);
                    // Must remove with the destroyed (fake-null) reference itself or it leaks in the set forever.
                    objectsInVolumeSet.Remove(wrapper);

                    if (objectsInVolume.Count == 0)
                        SetTriggerActivation(false);

                    continue;
                }

                if (!wrapper.gameObject.activeInHierarchy)
                    OnRigidbodyExit(null, wrapper);

                // TODO: Refactor the trigger volumes to use correct filtering logic.
                if (disableFilterRecheck)
                    continue;

                if (objectsInVolumeSet.Contains(wrapper)
                    && !CheckConditions(wrapper))
                {
                    ExitVolume(wrapper);
                }
            }
        }

        public void SetIgnored(Transform transform, bool ignore)
        {
            if (ignore)
            {
                if (!ignoredObjects.Contains(transform))
                    ignoredObjects.Add(transform);
            }
            else
            {
                ignoredObjects.Remove(transform);
            }
        }

        private bool CheckConditions(Rigidbody rigidbodyWrapper)
        {
            if (triggerIfBelowVelocity &&
                (rigidbodyWrapper.linearVelocity.magnitude > velocityThreshold ||
                 rigidbodyWrapper.angularVelocity.magnitude > angularVelocityThreshold))
            {
                return false;
            }

            if (!allowKinematics && rigidbodyWrapper.isKinematic)
                return false;

            if (ShouldIgnore(rigidbodyWrapper.transform))
                return false;

            if (useTagFiltering && _tagRegistryService.Exists)
            {
                // Untagged objects still run through the query with an empty list so positive filters reject them.
                _tagRegistryService.Reference.TryGetTagsFromGameObject(rigidbodyWrapper.gameObject, out IReadOnlyList<Tag> tags);
                if (!tagFilter.Matches(tags))
                    return false;
            }

            return true;
        }

        #region On Enter

        protected void OnTriggerEnter(Collider targetCollider)
        {
            if (targetCollider.isTrigger && !includeOtherTriggerColliders)
                return;

            if (!validLayers.Contains(targetCollider.gameObject.layer))
                return;

            OnColliderEnter(targetCollider);
        }

        private bool IsColliderValid(Collider targetCollider)
        {
            if (colliderFilter == null)
                return true;

            return colliderFilter(targetCollider);
        }

        private void OnColliderEnter(Collider collider)
        {
            if (!IsColliderValid(collider))
                return;
            
            Rigidbody rootRigidbodyWrapper = collider.attachedRigidbody;
            if (rootRigidbodyWrapper == null)
                return;

            if (!IsTransformEligible(rootRigidbodyWrapper.transform))
                return;

            if (AddColliderToEntry(rootRigidbodyWrapper, collider).HasValue)
                return;

            OnRigidbodyEnter(collider, rootRigidbodyWrapper);

            AddColliderToEntry(rootRigidbodyWrapper, collider);
        }

        private void OnRigidbodyEnter(Collider targetCollider, Rigidbody targetWrapper)
        {
            if (objectsInTrigger.Contains(targetWrapper))
                return;

            if (ShouldIgnore(targetWrapper.transform))
                return;

            objectsInTrigger.Add(new ObjectInTriggerVolumeData(targetWrapper));

#if DEBUG
            if (debug)
            {
                LogEvent("Enter Trigger", targetWrapper, targetCollider);
            }
#endif

            EnterVolume(targetWrapper);
        }

        #endregion On Enter

        private int? AddColliderToEntry(Rigidbody rigidbody, Collider collider)
        {
            int index = objectsInTrigger.IndexOf(rigidbody);
            if (index >= 0)
            {
                ObjectInTriggerVolumeData entry = objectsInTrigger[index];
                entry.collidersInVolume.Add(collider);
                objectsInTrigger[index] = entry;
                return entry.collidersInVolume.Count;
            }
            return null;
        }

        private int? RemoveColliderFromEntry(Rigidbody rigidbody, Collider collider)
        {
            int index = objectsInTrigger.IndexOf(rigidbody);
            if (index >= 0)
            {
                ObjectInTriggerVolumeData entry = objectsInTrigger[index];
                entry.collidersInVolume.Remove(collider);
                objectsInTrigger[index] = entry;
                return entry.collidersInVolume.Count;
            }
            return null;
        }

        protected void OnTriggerExit(Collider collider)
        {
            if (collider.isTrigger && !includeOtherTriggerColliders)
                return;

            if (!validLayers.Contains(collider.gameObject.layer))
                return;

            OnColliderExit(collider);
        }

        private void OnColliderExit(Collider targetCollider, Rigidbody targetRigidbody = null)
        {
            targetRigidbody ??= targetCollider.attachedRigidbody;
            if (targetRigidbody == null)
            {
                objectsInVolumeSet.Remove(null);
                objectsInVolume.Remove(null);
                return;
            }

            if (!IsTransformEligible(targetRigidbody.transform))
                return;

            int? result = RemoveColliderFromEntry(targetRigidbody, targetCollider);
            if (result.HasValue)
            {
                if (result > 0)
                    return;
            }

            RemoveRigidbodyAndChildrenFromTrigger(targetRigidbody, targetCollider);
        }

        public void RemoveRigidbodyAndChildrenFromTrigger(Rigidbody targetRigidbody, Collider targetCollider = null)
        {
            OnRigidbodyExit(targetCollider, targetRigidbody);
        }

        private void OnRigidbodyExit(Collider targetCollider, Rigidbody targetRigidbody)
        {
            // Double check if object is removed from volume list
            ExitVolume(targetRigidbody);

            int index = objectsInTrigger.IndexOf(targetRigidbody);
            if (index < 0)
            {
                return;
            }

            objectsInTrigger[index].Dispose();
            objectsInTrigger.RemoveAt(index);

#if DEBUG
            if (debug)
            {
                LogEvent("Exit Trigger", targetRigidbody, targetCollider);
            }
#endif

            ExitVolume(targetRigidbody);
        }

        /// <summary>
        /// Checks if transform is eligible to be detected by this trigger volume
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private bool IsTransformEligible(Transform other)
        {
            if (other == transform.parent || other == transform)
            {
                return false;
            }

            if (transform.IsChildOf(other) || other.IsChildOf(transform))
            {
                return false;
            }

            return true;
        }

        private void EnterVolume(Rigidbody targetRigidbody)
        {
            if (!CheckConditions(targetRigidbody))
                return;

            if (objectsInVolumeSet.Contains(targetRigidbody))
                return;

            objectsInVolume.Add(targetRigidbody);
            objectsInVolumeSet.Add(targetRigidbody);

            onObjectEnterVolume?.Invoke(targetRigidbody);

            if (objectsInVolume.Count >= 1)
                SetTriggerActivation(true);

#if DEBUG
            if (debug)
            {
                LogEvent("Enter Volume", targetRigidbody, null);
            }
#endif
        }

        private void SetTriggerActivation(bool activate)
        {
            if (activate)
                StartTrigger();
            else
                StopTrigger();
        }

        private void ExitVolume(Rigidbody targetRigidbody)
        {
            if (!objectsInVolume.Contains(targetRigidbody))
                return;

            objectsInVolume.Remove(targetRigidbody);
            objectsInVolumeSet.Remove(targetRigidbody);

            if (!targetRigidbody.IsNull())
                onObjectExitVolume?.Invoke(targetRigidbody);

            if (objectsInVolume.Count == 0)
                SetTriggerActivation(false);

#if DEBUG
            if (debug)
            {
                LogEvent("Exit Volume", targetRigidbody, null);
            }
#endif
        }

        public void ClearAllObjectsInVolume()
        {
            int count = objectsInVolume.Count;
            for (int i = count - 1; i >= 0; i--)
                ExitVolume(objectsInVolume[i]);
            objectsInTrigger.Clear();
            objectsInVolume.Clear();
            objectsInVolumeSet.Clear();
        }

        public abstract Vector3 GetTriggerOrigin();
        public abstract Bounds GetTriggerBounds();

        protected T CreateCollider<T>() where T : Collider
        {
            if (GetComponent<Collider>() == null && GetComponent<Rigidbody>() == null)
            {
                triggerChild = gameObject;
            }
            else
            {
                Transform triggerHelper = transform.Find(TRIGGER_HELPER_NAME);
                if (triggerHelper == null)
                {
                    // Create new empty game object
                    triggerChild = new GameObject
                    {
                        transform =
                        {
                            // Set some transform values for easy use later...
                            parent = transform,
                            localPosition = Vector3.zero,
                            localRotation = Quaternion.identity,
                            localScale = Vector3.one
                        },
                        name = TRIGGER_HELPER_NAME,
                        layer = overrideTriggerLayer ? triggerLayer : gameObject.layer
                    };
                }
                else
                {
                    triggerChild = triggerHelper.gameObject;
                }
            }

            T result = triggerChild.AddComponent<T>();
            triggerCollider = result;

            MeshCollider meshCollider = triggerCollider as MeshCollider;
            if (meshCollider != null)
                meshCollider.convex = true;

            triggerCollider.isTrigger = true;
            triggerCollider.enabled = enabled;
            return result;
        }

        #region Gizmos

        protected void SetTriggerCollider(Collider collider)
        {
            triggerCollider = collider;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;
            DrawColliderLines();
        }

        /// <summary>
        /// Draws the lines to all colliders in the volume
        /// </summary>
        protected void DrawColliderLines()
        {
            Vector3 startPosition = transform.position;

            for (int i = 0; i < objectsInVolume.Count; i++)
            {
                if (objectsInVolume[i] == null)
                    continue;

                Vector3 endPosition = objectsInVolume[i].position;

                Debug.DrawLine(startPosition, endPosition, Color.green);
            }
        }

        #endregion Gizmos

        private void LogEvent(string eventName, Rigidbody targetRigidbody, Collider collider)
        {
            Debug.LogFormat("<color=#{0}>{1}</color>: <color=#{2}>{3}</color> <color=#00ff80>{4}</color>, {5}",
                ColorUtility.ToHtmlStringRGB(ColorHelpers.GetUniqueColor(gameObject)), name,
                ColorUtility.ToHtmlStringRGB(ColorHelpers.GetUniqueColor(eventName)), eventName,
                targetRigidbody.name, collider);
        }
    }
}
