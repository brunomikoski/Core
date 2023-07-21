using System;
using System.Collections.Generic;
using BrunoMikoski.Core.Attributes;
using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    public abstract class TriggerVolumeBase : MonoBehaviour
    {
        private const string TRIGGER_HELPER_NAME = "TriggerHelper";

        [Header("Trigger Volume")]
        [SerializeField] 
        private Color gizmoColor = Color.cyan;
        [SerializeField] 
        private bool debug;

        [SerializeField] 
        private bool allowKinematics = true;

        [SerializeField]
        [Tooltip("Only adds objects to the volume if they are below a certain velocity")]
        private bool triggerIfBelowVelocity;

        [SerializeField, HideIf("triggerIfBelowVelocity", false)]
        private float velocityThreshold = 2.0f;

        [SerializeField, HideIf("triggerIfBelowVelocity", false)]
        private float angularVelocityThreshold = 2.0f;

        [SerializeField]
        private bool includeOtherTriggerColliders;


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
        private List<ObjectInTriggerVolumeData> objectsInTrigger =
            new List<ObjectInTriggerVolumeData>();

        public List<ObjectInTriggerVolumeData> ObjectsInTrigger => objectsInTrigger;

        /// <summary>
        /// Contains all physics objects inside the volume
        /// </summary>
        private List<Rigidbody> objectsInVolume = new List<Rigidbody>();

        public List<Rigidbody> ObjectsInVolume => objectsInVolume;

        //Used for quick lookup if the objects is already in the volume list (list.contains is to slow for every frame)
        private readonly HashSet<Rigidbody> objectsInVolumeSet =
            new HashSet<Rigidbody>();

        private readonly List<Transform> ignoredObjects = new List<Transform>();

        private ColliderFilterDelegate colliderFilter;
        public ColliderFilterDelegate ColliderFilter => colliderFilter;

        public delegate bool ColliderFilterDelegate(Collider collider);

        public Action<Rigidbody> OnObjectEnterVolumeEvent;
        public Action<Rigidbody> OnObjectExitVolumeEvent;

        /// <summary>
        /// Careful when using this event! This event is dispatched after Unity has registered it
        /// as being destroyed, but the targetCollider is still accessible. You can use it for cleanup
        /// as long as you don't require MonoBehaviour
        /// </summary>
        public Action<Rigidbody> OnObjectDestroyedInVolumeEvent;

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

                Rigidbody rigidbody = objectsInTrigger[i];
                if (rigidbody == null) // We cannot use .IsNull(), since we want
                    // to check if the object has been destroyed
                {
                    objectsInTrigger[i].Dispose();
                    objectsInTrigger.RemoveAt(i);
                    continue;
                }

                List<Collider> colliders = objectsInTrigger[i].collidersInVolume;
                for (int j = colliders.Count - 1; j >= 0; j--)
                {
                    if (colliders[j].enabled && colliders[j].gameObject.activeInHierarchy)
                        continue;

                    OnColliderExit(colliders[j], rigidbody);
                }
            }

            for (int i = objectsInVolume.Count - 1; i >= 0; i--)
            {
                Rigidbody rigidbody = objectsInVolume[i];
                if (rigidbody == null) // We cannot use .IsNull(), since we want
                    // to check if the object has been destroyed
                {
                    OnObjectDestroyedInVolumeEvent?.Invoke(rigidbody);
                    objectsInVolume.RemoveAt(i);

                    continue;
                }

                if (!rigidbody.gameObject.activeInHierarchy)
                    OnRigidbodyExit(null, rigidbody);
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

        private bool CheckConditions(Rigidbody rigidbody)
        {
            if (triggerIfBelowVelocity &&
                (rigidbody.velocity.magnitude > velocityThreshold ||
                 rigidbody.angularVelocity.magnitude > angularVelocityThreshold))
                return false;

            if (!allowKinematics && rigidbody.isKinematic)
                return false;

            if (ShouldIgnore(rigidbody.transform))
                return false;

            return true;
        }

        #region On Enter

        protected void OnTriggerEnter(Collider targetCollider)
        {
            if (targetCollider.isTrigger && !includeOtherTriggerColliders)
                return;

            OnColliderEnter(targetCollider);
        }

        private bool IsColliderValid(Collider targetCollider)
        {
            if (colliderFilter == null)
                return true;

            return colliderFilter(targetCollider);
        }

        private void OnColliderEnter(Collider targetCollider)
        {
            if (!IsColliderValid(targetCollider))
                return;

            Rigidbody rootRigidbody = targetCollider.attachedRigidbody;
            if (rootRigidbody == null)
                return;

            if (!IsTransformEligible(rootRigidbody.transform))
                return;

            if (AddColliderToEntry(rootRigidbody, targetCollider).HasValue)
                return;

            OnRigidbodyEnter(targetCollider, rootRigidbody);

            AddColliderToEntry(rootRigidbody, targetCollider);
        }

        private void OnRigidbodyEnter(Collider collider, Rigidbody rigidbody)
        {
            if (objectsInTrigger.Contains(rigidbody))
                return;

            if (!CheckConditions(rigidbody))
                return;

            objectsInTrigger.Add(new ObjectInTriggerVolumeData(rigidbody));

            EnterVolume(rigidbody);
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

        private static Rigidbody GetRigidbodyParent(Rigidbody rigidbody)
        {
            if (rigidbody.transform.parent == null)
                return null;

            return rigidbody.transform.parent.GetComponentInParent<Rigidbody>();
        }

        protected void OnTriggerExit(Collider collider)
        {
            if (collider.isTrigger && !includeOtherTriggerColliders)
                return;

            OnColliderExit(collider);
        }

        private void OnColliderExit(Collider targetCollider, Rigidbody rigidbody = null)
        {
            // ReSharper disable once Unity.NoNullCoalescing
            if (rigidbody == null)
                rigidbody = targetCollider.attachedRigidbody;
            
            if (rigidbody == null)
            {
                objectsInVolumeSet.Remove(null);
                objectsInVolume.Remove(null);
                return;
            }

            if (!IsTransformEligible(rigidbody.transform))
                return;

            int? result = RemoveColliderFromEntry(rigidbody, targetCollider);
            if (result.HasValue)
            {
                if (result > 0)
                    return;
            }

            RemoveRigidbodyAndChildrenFromTrigger(rigidbody, targetCollider);
        }

        private void RemoveRigidbodyAndChildrenFromTrigger(Rigidbody rigidbody, Collider collider = null)
        {
            Rigidbody RigidbodyParent = GetRigidbodyParent(rigidbody);
            if (RigidbodyParent.IsNotNull())
            {
                if (objectsInTrigger.Contains(RigidbodyParent))
                    return;
            }

            OnRigidbodyExit(collider, rigidbody);
        }

        private void OnRigidbodyExit(Collider collider, Rigidbody Rigidbody)
        {
            // Double check if object is removed from volume list
            ExitVolume(Rigidbody);

            int index = objectsInTrigger.IndexOf(Rigidbody);
            if (index < 0)
            {
                return;
            }

            objectsInTrigger[index].Dispose();
            objectsInTrigger.RemoveAt(index);

            ExitVolume(Rigidbody);
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

        private void EnterVolume(Rigidbody rigidbody)
        {
            if (!CheckConditions(rigidbody))
                return;

            if (objectsInVolumeSet.Contains(rigidbody))
                return;

            objectsInVolume.Add(rigidbody);
            objectsInVolumeSet.Add(rigidbody);

            OnObjectEnterVolumeEvent?.Invoke(rigidbody);

        }

        private void ExitVolume(Rigidbody rigidbody)
        {
            if (!objectsInVolume.Contains(rigidbody))
                return;

            objectsInVolume.Remove(rigidbody);
            objectsInVolumeSet.Remove(rigidbody);

            if (!rigidbody.IsNull())
                OnObjectExitVolumeEvent?.Invoke(rigidbody);
        }

        public void ClearAllObjectsInVolume()
        {
            int count = objectsInVolume.Count;
            for (int i = count - 1; i >= 0; i--)
                ExitVolume(objectsInVolume[i]);
            objectsInTrigger.Clear();
            objectsInVolume.Clear();
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
                    triggerChild = new GameObject();

                    // Set some transform values for easy use later...
                    triggerChild.transform.parent = transform;
                    triggerChild.transform.localPosition = Vector3.zero;
                    triggerChild.transform.localRotation = Quaternion.identity;
                    triggerChild.transform.localScale = Vector3.one;

                    triggerChild.name = TRIGGER_HELPER_NAME;

                    triggerChild.layer = gameObject.layer;
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

        #endregion Gizmos
   }
}
