using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    public abstract class TriggerVolume2DBase : MonoBehaviour
    {
        private const string TRIGGER_HELPER_NAME = "TriggerHelper";

        [Header("Gizmos")] 
        [SerializeField] 
        protected Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

        [Header("Trigger Volume")] 
        [SerializeField]
        private bool allowKinematics = true;

        protected GameObject triggerChild;
        private Collider2D triggerCollider;

        public Collider2D TriggerCollider
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
        private List<Object2DInTriggerVolumeData> objectsInTrigger = new();

        public List<Object2DInTriggerVolumeData> ObjectsInTrigger => objectsInTrigger;

        /// <summary>
        /// Contains all physics objects inside the volume
        /// </summary>
        private List<Rigidbody2D> objectsInVolume = new();

        public List<Rigidbody2D> ObjectsInVolume => objectsInVolume;

        //Used for quick lookup if the objects is already in the volume list (list.contains is to slow for every frame)
        private readonly HashSet<Rigidbody2D> objectsInVolumeSet = new();

        private readonly List<Transform> ignoredObjects = new();

        public Action<Rigidbody2D> OnObjectEnterVolumeEvent;
        public Action<Rigidbody2D> OnObjectExitVolumeEvent;

        /// <summary>
        /// Careful when using this event! This event is dispatched after Unity has registered it
        /// as being destroyed, but the targetCollider is still accessible. You can use it for cleanup
        /// as long as you don't require MonoBehaviour
        /// </summary>
        public Action<Rigidbody2D> OnObjectDestroyedInVolumeEvent;

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

        private bool ShouldIgnore(Transform targetTransform)
        {
            for (int i = ignoredObjects.Count - 1; i >= 0; i--)
            {
                if (ignoredObjects[i] == null)
                {
                    ignoredObjects.RemoveAt(i);
                    continue;
                }

                if (targetTransform.IsChildOf(ignoredObjects[i]))
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

                Rigidbody2D targetRigidbody = objectsInTrigger[i];
                if (targetRigidbody == null) // We cannot use .IsNull(), since we want
                    // to check if the object has been destroyed
                {
                    objectsInTrigger[i].Dispose();
                    objectsInTrigger.RemoveAt(i);
                    continue;
                }

                List<Collider2D> colliders = objectsInTrigger[i].collidersInVolume;
                for (int j = colliders.Count - 1; j >= 0; j--)
                {
                    if (colliders[j].enabled && colliders[j].gameObject.activeInHierarchy)
                        continue;

                    OnColliderExit(colliders[j], targetRigidbody);
                }
            }

            for (int i = objectsInVolume.Count - 1; i >= 0; i--)
            {
                Rigidbody2D targetRigidbody = objectsInVolume[i];
                if (targetRigidbody == null) // We cannot use .IsNull(), since we want
                    // to check if the object has been destroyed
                {
                    OnObjectDestroyedInVolumeEvent?.Invoke(targetRigidbody);
                    objectsInVolume.RemoveAt(i);

                    continue;
                }

                if (!targetRigidbody.gameObject.activeInHierarchy)
                    OnRigidbodyExit(targetRigidbody);
            }
        }

        public void SetIgnored(Transform targetTransform, bool ignore)
        {
            if (ignore)
            {
                if (!ignoredObjects.Contains(targetTransform))
                    ignoredObjects.Add(targetTransform);
            }
            else
            {
                ignoredObjects.Remove(targetTransform);
            }
        }

        private bool CheckConditions(Rigidbody2D targetRigidbody)
        {
            if (!allowKinematics && targetRigidbody.isKinematic)
                return false;

            if (ShouldIgnore(targetRigidbody.transform))
                return false;

            return true;
        }

        #region On Enter

        protected void OnTriggerEnter2D(Collider2D targetCollider)
        {
            if (targetCollider.isTrigger)
                return;

            OnColliderEnter2D(targetCollider);
        }

        private void OnColliderEnter2D(Collider2D targetCollider)
        {
            Rigidbody2D rootRigidbody = targetCollider.attachedRigidbody;
            if (rootRigidbody == null)
                return;

            if (!IsTransformEligible(rootRigidbody.transform))
                return;

            if (AddColliderToEntry(rootRigidbody, targetCollider).HasValue)
                return;

            OnRigidbodyEnter(rootRigidbody);

            AddColliderToEntry(rootRigidbody, targetCollider);
        }

        private void OnRigidbodyEnter(Rigidbody2D targetRigidbody)
        {
            if (objectsInTrigger.Contains(targetRigidbody))
                return;

            if (!CheckConditions(targetRigidbody))
                return;

            objectsInTrigger.Add(new Object2DInTriggerVolumeData(targetRigidbody));

            EnterVolume(targetRigidbody);
        }

        #endregion On Enter

        private int? AddColliderToEntry(Rigidbody2D targetRigidbody, Collider2D targetCollider)
        {
            int index = objectsInTrigger.IndexOf(targetRigidbody);
            if (index < 0)
                return null;

            Object2DInTriggerVolumeData entry = objectsInTrigger[index];
            entry.collidersInVolume.Add(targetCollider);
            objectsInTrigger[index] = entry;
            return entry.collidersInVolume.Count;
        }

        private int? RemoveColliderFromEntry(Rigidbody2D targetRigidbody, Collider2D targetCollider)
        {
            int index = objectsInTrigger.IndexOf(targetRigidbody);
            if (index < 0)
                return null;

            Object2DInTriggerVolumeData entry = objectsInTrigger[index];
            entry.collidersInVolume.Remove(targetCollider);
            objectsInTrigger[index] = entry;
            return entry.collidersInVolume.Count;
        }

        private static Rigidbody2D GetRigidbodyParent(Rigidbody2D targetRigidbody)
        {
            if (targetRigidbody.transform.parent == null)
                return null;

            return targetRigidbody.transform.parent.GetComponentInParent<Rigidbody2D>();
        }

        protected void OnTriggerExit2D(Collider2D targetCollider)
        {
            if (targetCollider.isTrigger)
                return;

            OnColliderExit(targetCollider);
        }

        private void OnColliderExit(Collider2D targetCollider, Rigidbody2D targetRigidbody = null)
        {
            // ReSharper disable once Unity.NoNullCoalescing
            if (targetRigidbody == null)
                targetRigidbody = targetCollider.attachedRigidbody;

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

            RemoveRigidbodyAndChildrenFromTrigger(targetRigidbody);
        }

        private void RemoveRigidbodyAndChildrenFromTrigger(Rigidbody2D targetRigidbody)
        {
            Rigidbody2D rigidbodyParent = GetRigidbodyParent(targetRigidbody);
            if (rigidbodyParent.IsNotNull())
            {
                if (objectsInTrigger.Contains(rigidbodyParent))
                    return;
            }

            OnRigidbodyExit(targetRigidbody);
        }

        private void OnRigidbodyExit(Rigidbody2D targetRigidbody)
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

        private void EnterVolume(Rigidbody2D targetRigidbody)
        {
            if (!CheckConditions(targetRigidbody))
                return;

            if (objectsInVolumeSet.Contains(targetRigidbody))
                return;

            objectsInVolume.Add(targetRigidbody);
            objectsInVolumeSet.Add(targetRigidbody);

            OnObjectEnterVolumeEvent?.Invoke(targetRigidbody);
        }

        private void ExitVolume(Rigidbody2D targetRigidbody)
        {
            if (!objectsInVolume.Contains(targetRigidbody))
                return;

            objectsInVolume.Remove(targetRigidbody);
            objectsInVolumeSet.Remove(targetRigidbody);

            if (!targetRigidbody.IsNull())
                OnObjectExitVolumeEvent?.Invoke(targetRigidbody);
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

        protected T CreateCollider<T>() where T : Collider2D
        {
            if (GetComponent<Collider2D>() == null && GetComponent<Rigidbody2D>() == null)
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
                        layer = gameObject.layer
                    };
                }
                else
                {
                    triggerChild = triggerHelper.gameObject;
                }
            }

            T result = triggerChild.AddComponent<T>();
            triggerCollider = result;

            triggerCollider.isTrigger = true;
            triggerCollider.enabled = enabled;
            return result;
        }

        protected void SetTriggerCollider(Collider2D targetCollider)
        {
            triggerCollider = targetCollider;
        }
    }
}
