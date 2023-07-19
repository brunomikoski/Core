using System;
using System.Collections.Generic;
using BrunoMikoski.AnimationSequencer;
#if DOTWEEN_ENABLED
using DG.Tweening;
#endif
using UnityEngine;
using UnityEngine.Assertions;

namespace BrunoMikoski.Core.PhysX
{
    [RequireComponent(typeof(RigidbodyWrapper))]
    public class ReparentableRigidbody : MonoBehaviour
    {
        public const float REPARENT_ANIMATION_DURATION = 0.2f;

        private struct ColliderTransformData
        {
            public Transform parent;
            public Vector3 beforeLocalPosition;
            public Vector3 initialLocalPosition;
            public Quaternion localRotation;
            public Quaternion initialLocalRotation;
            public Vector3 initialLocalScale;
        }

        public IList<Collider> Colliders => RigidbodyWrapper.Colliders;

        [SerializeField] 
        private bool animateReparenting;
        [SerializeField]
        private CustomEase animationEase = CustomEase.OutQuad;

        [SerializeField] private bool turnOffCollidersOnReparent;
        private bool originalColliderBehaviour;
        public bool TurnOffCollidersOnReparent
        {
            get => turnOffCollidersOnReparent;
            set => turnOffCollidersOnReparent = value;
        }

        private ColliderTransformData[] colliderTransformData;

        private Transform originalParent;

        private readonly Dictionary<ReparentableRigidbody, Transform> childToTargetTransform = new();

        private bool isEnabledBeforeReparenting;
        private bool isKinematicBeforeReparenting;
        private Vector3 initialScale;
#if DOTWEEN_ENABLED
        private Tween reparentPositionTween;
        private Tween reparentRotationTween;
#endif
        public bool IsAnimatingReparent =>
            reparentPositionTween != null && reparentRotationTween != null
                                          && ((reparentPositionTween.IsActive() && reparentPositionTween.IsPlaying())
                                              || (reparentRotationTween.IsActive() && reparentRotationTween.IsPlaying()));

        private RigidbodyInterpolation interpolationBeforeReparenting;

        private ReparentableRigidbody reparentedToReparentable;
        
        private RigidbodyWrapper reparentedTo;
        public RigidbodyWrapper ReparentedTo => reparentedTo;


        private bool hasCachedRigidbodyWrapper;
        private RigidbodyWrapper cachedRigidbodyWrapper;
        public RigidbodyWrapper RigidbodyWrapper
        {
            get
            {
                if (!hasCachedRigidbodyWrapper)
                {
                    cachedRigidbodyWrapper = GetComponent<RigidbodyWrapper>();
                    hasCachedRigidbodyWrapper = cachedRigidbodyWrapper != null;
                }
                return cachedRigidbodyWrapper;
            }
        }

        private bool hasCachedRigidbody;
        private Rigidbody cachedRigidbody;
        public Rigidbody Rigidbody
        {
            get
            {
                if (!hasCachedRigidbody)
                {
                    cachedRigidbody = GetComponent<Rigidbody>();
                    hasCachedRigidbody = cachedRigidbody != null;
                }
                return cachedRigidbody;
            }
        }

        public Action<ReparentableRigidbody> OnReparentedEvent;

        private bool isDestroying;


        private bool hasCachedTransform;
        private Transform cachedTransform;
        private Transform Transform
        {
            get
            {
                if (!hasCachedTransform)
                {
                    cachedTransform = transform;
                    hasCachedTransform = true;
                }

                return cachedTransform;
            }
        }

        private void Initialize()
        {
            if (colliderTransformData != null)
                return;

            // Make sure our rigidbodywrapper's components are cached while its hierarchy is intact.
            RigidbodyWrapper.Initialize();

            initialScale = RigidbodyWrapper.transform.localScale;
            colliderTransformData = new ColliderTransformData[Colliders.Count];

            for (int i = 0; i < Colliders.Count; i++)
            {
                Collider targetCollider = Colliders[i];
                if (targetCollider == null)
                    continue;

                Transform targetColliderTransform = targetCollider.transform;
                Vector3 transformLocalPosition = targetColliderTransform.localPosition;
                colliderTransformData[i] = new ColliderTransformData()
                {
                    parent = targetColliderTransform.parent,
                    beforeLocalPosition = transformLocalPosition,
                    localRotation = targetColliderTransform.localRotation,
                    initialLocalScale = targetColliderTransform.localScale
                };
            }
            originalColliderBehaviour = turnOffCollidersOnReparent;
        }

        private void OnDestroy()
        {
            isDestroying = true;

            // If this didn't get unparented we're cleaning up the data at least
            if (reparentedTo.IsNotNull())
                reparentedTo.UnregisterChild(this);

            // Make sure that if the rigidbodywrapper gets destroyed, we also destroy any colliders
            // that might not have been unparented yet.
            if (Colliders != null)
            {
                for (int i = 0; i < Colliders.Count; i++)
                {
                    if (Colliders[i] != null)
                        Destroy(Colliders[i].gameObject);
                }
            }
        }

        public void ReparentTo(RigidbodyWrapper target, bool keepWorldPosition,
            Transform targetTransform = null)
        {
            ReparentTo(target, keepWorldPosition, targetTransform, false, default(Vector3),
                default(Quaternion), false);
        }

        public void ReparentTo(RigidbodyWrapper target, bool keepWorldPosition,
            Transform targetTransform, Vector3 localPosition, Quaternion localRotation, bool forceNoAnimation = false)
        {
            ReparentTo(target, keepWorldPosition, targetTransform, true, localPosition,
                localRotation, forceNoAnimation);
        }

        public void ReparentTo(RigidbodyWrapper target, bool keepWorldPosition, Transform targetTransform,
            bool resetLocal, Vector3 localPosition, Quaternion localRotation, bool forceNoAnimation = false,
            bool forceCollidersOff = false)
        {
            Initialize();

            // Make sure that our target's rigidbodywrapper's components are cached while its
            // hierarchy is still intact.
            if (target != null)
                target.Initialize();

            Assert.IsNull(reparentedTo);

            if (targetTransform == null)
            {
                targetTransform = target.transform;
            }
#if DEBUG
            Assert.IsTrue(targetTransform.IsChildOf(target.transform));
#endif
            turnOffCollidersOnReparent = forceCollidersOff || originalColliderBehaviour;

            reparentedToReparentable = target.GetComponent<ReparentableRigidbody>();
            if (reparentedToReparentable != null)
            {
                reparentedToReparentable.childToTargetTransform[this] = targetTransform;
            }

            isKinematicBeforeReparenting = Rigidbody.isKinematic;
            interpolationBeforeReparenting = Rigidbody.interpolation;

            if (originalParent == null)
            {
                originalParent = Transform.parent;
            }

            Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Rigidbody.isKinematic = true;
            Rigidbody.interpolation = RigidbodyInterpolation.None;

            Vector3 fromPosition = default;
            Quaternion fromRotation = default;
            if (animateReparenting)
            {
#if DOTWEEN_ENABLED
                Transform.DOKill();
#endif
                fromPosition = Transform.position;
                fromRotation = Transform.rotation;
            }

            Transform.SetParent(targetTransform, keepWorldPosition);

            if (resetLocal)
            {
                Transform.localPosition = localPosition;
                Transform.localRotation = localRotation;
            }

            reparentedTo = target;
            target.RegisterChild(this);

            isEnabledBeforeReparenting = RigidbodyWrapper.CollidersEnabled;
            if (!reparentedTo.CollidersEnabled)
                UpdateCollidersEnabledState(false, true);

            Transform colliderTargetTransform = reparentedToReparentable == null
                ? targetTransform
                : reparentedToReparentable.GetColliderTransform(targetTransform);

            for (int i = 0; i < Colliders.Count; i++)
            {
                ColliderTransformData transformData = colliderTransformData[i];
                transformData.initialLocalPosition = Transform.InverseTransformPoint(Colliders[i].transform.position);
                transformData.initialLocalRotation =
                    Quaternion.Inverse(Transform.rotation) * Colliders[i].transform.rotation;
                colliderTransformData[i] = transformData;
            }

            ReparentCollidersTo(colliderTargetTransform);

            if (animateReparenting && !forceNoAnimation)
            {
                Vector3 toPosition = Transform.localPosition;
                Quaternion toRotation = Transform.localRotation;
                Transform.position = fromPosition;
                Transform.rotation = fromRotation;
#if DOTWEEN_ENABLED
                reparentPositionTween = Transform.DOLocalMove(toPosition, REPARENT_ANIMATION_DURATION)
                    .SetEase(animationEase);
                reparentRotationTween = Transform.DOLocalRotate(toRotation.eulerAngles, REPARENT_ANIMATION_DURATION)
                    .SetEase(animationEase);
#else
                Transform.localPosition = toPosition;
                Transform.localRotation = toRotation;
#endif
            }

            OnReparentedEvent?.Invoke(this);
        }

        private Transform GetColliderTransform(Transform fallbackTarget)
        {
            if (reparentedTo == null)
                return fallbackTarget;

            if (reparentedToReparentable != null)
                return reparentedToReparentable.GetColliderTransform(Transform.parent);

            return Transform.parent;
        }

        private void ReparentCollidersTo(Transform targetTransform, bool reApplyInitialValues = false)
        {
            for (int i = 0; i < Colliders.Count; i++)
            {
                Collider collider = Colliders[i];

                if (turnOffCollidersOnReparent)
                    collider.enabled = false;
                else
                {
                    Transform colliderTransform = collider.transform;
                    colliderTransform.SetParent(targetTransform, true);

                    if (reApplyInitialValues)
                    {
                        ColliderTransformData transformData = colliderTransformData[i];
                        colliderTransform.localPosition = transformData.initialLocalPosition;
                        colliderTransform.localRotation = transformData.initialLocalRotation;
                        colliderTransform.localScale = transformData.initialLocalScale;
                    }
                }
            }

            if (childToTargetTransform == null)
                return;

            foreach (ReparentableRigidbody rigidbodyWrapper in childToTargetTransform.Keys)
            {
                rigidbodyWrapper.ReparentCollidersTo(targetTransform, reApplyInitialValues);
            }
        }

        public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation)
        {
            if (reparentedTo == null)
                return;

            Transform.localPosition = localPosition;
            Transform.localRotation = localRotation;
            
            for (int i = 0; i < Colliders.Count; i++)
            {
                Collider targetCollider = Colliders[i];

                if (turnOffCollidersOnReparent)
                {
                    targetCollider.enabled = false;
                }
                else
                {
                    ColliderTransformData transformData = colliderTransformData[i];
                    targetCollider.transform.localPosition = localPosition + (transformData.initialLocalPosition);
                    targetCollider.transform.localRotation = localRotation * transformData.initialLocalRotation;
                }
            }
        }

        public void UpdateScale()
        {
            initialScale = RigidbodyWrapper.transform.localScale;

            if (colliderTransformData == null)
                return;

            for (int i = 0; i < Colliders.Count; i++)
            {
                Transform colliderTransform = Colliders[i].transform;
                ColliderTransformData transformData = colliderTransformData[i];
                transformData.initialLocalScale = colliderTransform.localScale;
            }
        }

        public void Unparent(bool forceKillTweens = false)
        {
            if (animateReparenting || forceKillTweens)
            {
#if DOTWEEN_ENABLED
                Transform.DOKill();
#endif
            }

            if (isDestroying)
                return;

            if (reparentedToReparentable != null)
            {
                reparentedToReparentable.childToTargetTransform.Remove(this);
            }

            if (originalParent != null && !originalParent.gameObject.IsDontDestroyOnLoad())
            {
                Debug.LogWarning("Will not unparent to originalParent, since it will be destroyed", originalParent);
                return;
            }

            if (originalParent != null)
            {
                Transform.SetParent(originalParent, true);
            }
            else
            {
                Transform.SetParent(null, true);
            }

            Transform.localScale = initialScale;

            for (int i = 0; i < Colliders.Count; i++)
            {
                if (turnOffCollidersOnReparent)
                {
                    Colliders[i].enabled = true;
                }

                Transform colliderTransform = Colliders[i].transform;
                ColliderTransformData transformData = colliderTransformData[i];
                colliderTransform.SetParent(transformData.parent, true);
                colliderTransform.localPosition = transformData.beforeLocalPosition;
                colliderTransform.localRotation = transformData.localRotation;
                colliderTransform.localScale = transformData.initialLocalScale;
            }

            if (childToTargetTransform != null)
            {
                foreach (KeyValuePair<ReparentableRigidbody, Transform> kvp
                         in childToTargetTransform)
                {
                    kvp.Key.ReparentCollidersTo(kvp.Value, true);
                }
            }

            Rigidbody.isKinematic = isKinematicBeforeReparenting;
            Rigidbody.interpolation = interpolationBeforeReparenting;

            reparentedTo.UnregisterChild(this);

            if (isEnabledBeforeReparenting)
                UpdateCollidersEnabledState(true, true);

            reparentedTo = null;
            reparentedToReparentable = null;

            turnOffCollidersOnReparent = originalColliderBehaviour;

            RigidbodyWrapper.ReportUnparent();
            OnReparentedEvent?.Invoke(this);
        }

        private void UpdateCollidersEnabledState(bool enabled, bool includeChildren)
        {
            if (enabled && turnOffCollidersOnReparent && ReparentedTo != null)
                return;

            IList<Collider> colliders = RigidbodyWrapper.Colliders;
            for (int i = 0; i < colliders.Count; i++)
            {
                colliders[i].enabled = enabled;
            }

            if (!includeChildren)
                return;

            IList<ReparentableRigidbody> children = RigidbodyWrapper.Children;
            if (children == null)
                return;

            for (int i = 0; i < children.Count; i++)
            {
                children[i].UpdateCollidersEnabledState(enabled, true);
            }
        }

        public RigidbodyWrapper GetRoot()
        {
            if (reparentedToReparentable != null)
                return reparentedToReparentable.GetRoot();
            if (reparentedTo != null)
                return reparentedTo;
            return RigidbodyWrapper;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (Colliders == null || Colliders.Count == 0)
            {
                return;
            }

            for (int i = 0; i < Colliders.Count; i++)
            {
                Collider targetCollider = Colliders[i];

                if (targetCollider == null)
                    continue;

                if (targetCollider.transform == Transform)
                {
                    Debug.LogErrorFormat(this, "Colliders need to have their own GameObject "
                                               + "when reparentable. Path {0} Scene {1}", Transform.GetPath(),
                        gameObject.scene.path);
                }

                using (PoolList<Component> components = targetCollider.GetComponentsPooled<Component>())
                {
                    for (int j = 0; j < components.Count; j++)
                    {
                        Component component = components[j];
                        if (component is Transform || component is Collider
                                                   || component is IAllowWithReparentableCollider)
                        {
                            continue;
                        }

                        Debug.LogErrorFormat(this, "Collider GameObject has different types of objects other "
                                                   + "than colliders. Type: {0}. Path {1} Scene {2}",
                            component.GetType().Name, Transform.GetPath(), gameObject.scene.path);
                    }
                }
            }
        }

        public void SetAnimateReparenting(bool animateReparenting)
        {
            this.animateReparenting = animateReparenting;
        }
    }
}
