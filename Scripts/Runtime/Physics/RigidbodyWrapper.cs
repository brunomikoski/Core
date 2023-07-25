using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.ServicesLocation;
using UnityEngine;
using UnityEngine.Assertions;

namespace BrunoMikoski.Core.PhysX
{
    /// <summary>
    /// Wraps calls that were intended for a Rigidbody to allow it to be intercepted and handled
    /// differently for mechanics like time stop. Preferably you'd never speak to a Rigidbody
    /// directly and always use RigidbodyWrapper as the interface to the physics engine.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed partial class RigidbodyWrapper : MonoBehaviour
    {
        private const float CONTINUOUS_VELOCITY_THRESHOLD = 2f;

        [Header("Default Overrides")]
        [SerializeField] 
        private Transform centerOfMass;


        private RigidbodyWrapper controlledByRigidbody;

        //These values are set at initialization
        private int frameOfInitialization;
        private Transform initialParent;
        private Vector3 initialLocalScale;
        
        private float originalAngularDrag;
        public float OriginalAngularDrag => originalAngularDrag;

        private bool collidersEnabled = true;
        public bool CollidersEnabled => collidersEnabled;

        private List<ReparentableRigidbody> children = new();
        public IList<ReparentableRigidbody> Children => children;

        private bool needsColliderCacheRefreshOnUnparent;


        private bool hasCachedRenderer;
        private Renderer cachedRenderer;
        public Renderer Renderer
        {
            get
            {
                if (!hasCachedRenderer)
                {
                    cachedRenderer = GetComponentInChildren<Renderer>();
                    hasCachedRenderer = true;
                }
                return cachedRenderer;
            }
        }

        public Vector3 CenterOfVisuals
        {
            get
            {
                if (Renderer != null)
                    return Renderer.bounds.center;

                return WorldPosition;
            }
        }

        public Vector3 CenterOfColliders => GetWorldColliderBounds().center;

        private List<PhysicMaterial> cachedPhysicMaterials;
        private List<Collider> cachedColliders;

        private Bounds cachedLocalColliderBounds;

        public IList<Collider> Colliders
        {
            get
            {
                CacheColliders();
                return cachedColliders;
            }
        }

        private IRigidbodyMaster master;
        public IRigidbodyMaster Master => master;
        public RigidbodyWrapper Root => master == null ? this : master.RigidbodyWrapper.Root;

        private bool isOwned;
        public bool IsOwned => isOwned;

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

        public RigidbodyInterpolation Interpolation => Rigidbody.interpolation;

        public Vector3 Position
        {
            get => Rigidbody.position;
            set => Rigidbody.position = value;
        }

        public Quaternion Rotation
        {
            get => Rigidbody.rotation;
            set => Rigidbody.rotation = value;
        }

        private Vector3 previousPosition;
        //This is the speed of the object per second
        private Vector3 measuredVelocity;
        public Vector3 MeasuredVelocity => measuredVelocity;

        public float Mass
        {
            get => Rigidbody.mass;
            set
            {
                float previousMass = Rigidbody.mass;

                Rigidbody.mass = MassScale * value;

                if (!Mathf.Approximately(previousMass, Rigidbody.mass))
                    OnMassChangedEvent?.Invoke(this);
            }
        }

        public float MassScale => Rigidbody.mass;

        public bool CanMove => !gameObject.isStatic && !Rigidbody.isKinematic;

        public Vector3 WorldCenterOfMass => Rigidbody.worldCenterOfMass;

        public Vector3 CenterOfMass
        {
            get => Rigidbody.centerOfMass;
            set => Rigidbody.centerOfMass = value;
        }

        public Transform CenterOfMassPivot
        {
            get => centerOfMass;
            set
            {
                centerOfMass = value;

                if (!Application.isPlaying)
                    return;

                customCenterOfMass = transform.InverseTransformPoint(centerOfMass.position);
                Rigidbody.centerOfMass = customCenterOfMass;
            }
        }

        private Vector3 originalCenterOfMass;
        public Vector3 OriginalCenterOfMass => originalCenterOfMass;

        private Vector3 customCenterOfMass;
        public Vector3 CustomCenterOfMass => customCenterOfMass;

        private Vector3 previousVelocity;
        public Vector3 PreviousVelocity => previousVelocity;

        private CollisionDetectionMode initialCollisionDetectionMode;

        public Vector3 Velocity
        {
            get => VelocityUnfiltered;
            set => VelocityUnfiltered = value;
        }

        public Vector3 VelocityUnfiltered
        {
            get => Rigidbody.velocity;
            set => Rigidbody.velocity = value;
        }

        public Vector3 AngularVelocity
        {
            get => AngularVelocityUnfiltered;
            set => AngularVelocityUnfiltered = value;
        }

        public Vector3 AngularVelocityUnfiltered
        {
            get => Rigidbody.angularVelocity;
            set => Rigidbody.angularVelocity = value;
        }

        public float Drag
        {
            get => Rigidbody.drag;
            set => Rigidbody.drag = value;
        }

        public float AngularDrag
        {
            get => Rigidbody.angularDrag;
            set => Rigidbody.angularDrag = value;
        }

        public bool IsKinematic
        {
            get => Rigidbody.isKinematic;
            set
            {
                if (value)
                {
                    if (Rigidbody.collisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative)
                    {
                        initialCollisionDetectionMode = Rigidbody.collisionDetectionMode;
                        Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }
                }
                bool wasKinematic = Rigidbody.isKinematic;
                Rigidbody.isKinematic = value;

                //Need to happen after the .isKinematic = false is set
                if (wasKinematic && !Rigidbody.isKinematic)
                    Rigidbody.WakeUp();

                if (!value && initialCollisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative)
                {
                    Rigidbody.collisionDetectionMode = initialCollisionDetectionMode;
                }
            }
        }

        public Vector3 InitialLocalScale => initialLocalScale;

        public Vector3 WorldPosition => transform.position;

        public Vector3 LocalPosition => transform.localPosition;
        public Quaternion LocalRotation => transform.localRotation;

        private RigidbodyInterpolation originalInterpolation;
        public RigidbodyInterpolation OriginalInterpolation => originalInterpolation;

        private RigidbodyConstraints originalConstraints;
        public RigidbodyConstraints Constraints => Rigidbody.constraints;

        // TODO: Move this to a Teleported physics state? Seems overkill for now.
        private float lastTeleportTime;
        public float LastTeleportTime => lastTeleportTime;

        private bool maxSpeedIsOverridden;
        private float maxSpeedOriginalDrag;

        public Transform InitialParent => initialParent;


        private bool forceContinuousDynamicCollisions;
        public bool ForceContinuousDynamicCollisions => forceContinuousDynamicCollisions;

        public Action<IRigidbodyMaster> OnStartOwnedEvent;

        public Action<IRigidbodyMaster> OnStopOwnedEvent;
        public Action<Collision> OnCollisionEnterEvent;
        public Action<RigidbodyWrapper> OnMassChangedEvent;
        public Action<RigidbodyWrapper, RigidbodyInterpolation> OnInterpolationChanged;

        
        private bool hasCacheReparentableRigidbody;
        private ReparentableRigidbody cachedReparentableRigidbody;
        public ReparentableRigidbody ReparentableRigidbody
        {
            get
            {
                if (!hasCacheReparentableRigidbody)
                {
                    cachedReparentableRigidbody = GetComponent<ReparentableRigidbody>();
                    hasCacheReparentableRigidbody = cachedReparentableRigidbody != null;
                }
                return cachedReparentableRigidbody;
            }
        }

        public bool IsReparented
        {
            get
            {
                if (ReparentableRigidbody.IsNull())
                    return false;

                return ReparentableRigidbody.ReparentedTo != null;
            }
        }

        private bool isInitialized;
        
        private ServiceReference<PhysicsService> physicsService = new();


        private void Awake()
        {
            if (Rigidbody == null)
            {
                // In that case this component isn't needed and is best off removing itself.
                DestroyImmediate(this);
                return;
            }

            originalCenterOfMass = Rigidbody.centerOfMass;

            if (centerOfMass != null)
            {
                customCenterOfMass = transform.InverseTransformPoint(centerOfMass.position);
                Rigidbody.centerOfMass = customCenterOfMass;
            }

            
            physicsService.Reference.RegisterPhysicsObject(this);

            Initialize();

            originalConstraints = Rigidbody.constraints;
            originalInterpolation = Rigidbody.interpolation;
            originalAngularDrag = AngularDrag;
        }
        /// <summary>
        /// Caches all components as needed. Hierarchy disrupting functionality such as reparenting
        /// should always call RigidbodyWrapper's initialize first so it has a chance to cache its
        /// components while the hierarchy is still intact. Will never execute twice.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            SetInitialValues();
        }

        private void OnDestroy()
        {
            if (physicsService.Exists)
                physicsService.CachedReference.UnregisterPhysicsObject(this);
        }

        public void PhysicsUpdate()
        {
            if (cachedRigidbody.IsSleeping())
            {
                previousVelocity = Vector3.zero;
                measuredVelocity = Vector3.zero;
                previousPosition = Position;
                return;
            }

            if (DetectInvalidPosition())
                return;

            measuredVelocity = (Position - previousPosition) / Time.fixedDeltaTime;
            previousPosition = Position;
            previousVelocity = Velocity;
            UpdateCollisionDetectionMode();
        }

        public void ReportUnparent()
        {
            if (needsColliderCacheRefreshOnUnparent)
                UpdateColliderCache();
        }

        /// <summary>
        /// Caches all the colliders. Call this if you're adding or removing colliders.
        /// </summary>
        public void CacheColliders(bool forceRebuild = false)
        {
            if (cachedColliders != null && !forceRebuild && Application.isPlaying)
                return;

            bool isReparented = false;
            PoolList<Collider> intactReparentedColliders = null;

            if (cachedColliders == null)
            {
                cachedColliders = new List<Collider>();
            }
            else
            {
                if (ReparentableRigidbody != null && ReparentableRigidbody.ReparentedTo != null)
                {
                    isReparented = true;
                    intactReparentedColliders = PoolList<Collider>.Obtain();
                    for (int i = 0; i < cachedColliders.Count; i++)
                    {
                        Collider collider = cachedColliders[i];
                        if (collider.attachedRigidbody == ReparentableRigidbody.GetRoot().Rigidbody)
                            intactReparentedColliders.Add(collider);
                    }
                }
            }

            GetComponentsInChildren(cachedColliders);

            if (intactReparentedColliders != null)
            {
                cachedColliders.AddRange(intactReparentedColliders);
                intactReparentedColliders.Dispose();
            }

            for (int i = cachedColliders.Count - 1; i >= 0; i--)
            {
                Collider cachedCollider = cachedColliders[i];
                if (cachedCollider.isTrigger || !cachedCollider.enabled)
                {
                    cachedColliders.RemoveAt(i);
                    continue;
                }

                using (PoolList<RigidbodyWrapper> parents =
                       cachedCollider.GetComponentsInParentPooled<RigidbodyWrapper>(true))
                {
                    RigidbodyWrapper parent = parents.First();
                    if (parent != this || parent == null || (parent.ReparentableRigidbody != null
                                                             && parent.ReparentableRigidbody.GetRoot() != this))
                    {
                        cachedColliders.RemoveAt(i);
                        continue;
                    }
                }

#if UNITY_EDITOR
                if (!Application.isPlaying && gameObject.IsPrefab())
                {
                    if (!cachedCollider.gameObject.IsActiveInPrefabHierarchy())
                        cachedColliders.RemoveAt(i);
                }
#endif
            }

            if (cachedPhysicMaterials == null)
                cachedPhysicMaterials = new List<PhysicMaterial>(cachedColliders.Count);
            else
                cachedPhysicMaterials.Clear();

            for (int i = 0; i < cachedColliders.Count; i++)
                cachedPhysicMaterials.Add(cachedColliders[i].sharedMaterial);

            if (!isReparented)
            {
                CacheLocalColliderBounds();
                return;
            }

            needsColliderCacheRefreshOnUnparent = true;
        }

        private void CacheLocalColliderBounds()
        {
            Bounds worldBounds = GetWorldColliderBounds();
            cachedLocalColliderBounds = transform.InverseTransformBounds(worldBounds);
        }

        private bool DetectInvalidPosition()
        {
            if (transform.position.y > -100f)
                return false;

            if (!IsKinematic && Application.isEditor)
            {
                Debug.LogError($"Object in a position where it probably shouldn't be! {transform.GetPath()}",
                    gameObject);
            }

            IsKinematic = true;
            return true;
        }

        public void MarkAsTeleported()
        {
            lastTeleportTime = Time.time;
        }

        private void UpdateCollisionDetectionMode()
        {
            CollisionDetectionMode currentDetectionMode = cachedRigidbody.collisionDetectionMode;
            CollisionDetectionMode detectionMode;

            if (!CanMove)
            {
                detectionMode = CollisionDetectionMode.Discrete;
            }
            else
            {
                Vector3 velocity = VelocityUnfiltered;
                float flatVelocity = Mathf.Abs(velocity.x + velocity.y + velocity.z);
                if (flatVelocity > CONTINUOUS_VELOCITY_THRESHOLD || forceContinuousDynamicCollisions)
                {
                    //Do continues checks against other rigidbodies that have continues or dynamic
                    detectionMode = CollisionDetectionMode.ContinuousDynamic;
                }
                else
                {
                    //Do continues checks against static objects or continues-dynamic objects
                    detectionMode = CollisionDetectionMode.Continuous;
                }
            }

            if (currentDetectionMode != detectionMode)
                cachedRigidbody.collisionDetectionMode = detectionMode;
        }

        private void SetInitialValues()
        {
            // Save the world position and rotation, as the parent's position/rotation could change
            frameOfInitialization = Time.frameCount;

            Transform myTransform = transform;
            initialParent = myTransform.parent;
            initialLocalScale = myTransform.localScale;
            initialCollisionDetectionMode = Rigidbody.collisionDetectionMode;

            CacheColliders();
        }

        public void UpdateColliderCache()
        {
            needsColliderCacheRefreshOnUnparent = false;
            CacheColliders(true);

            originalCenterOfMass = cachedLocalColliderBounds.center;

            if (centerOfMass == null)
            {
                Rigidbody.centerOfMass = originalCenterOfMass;
            }
        }

        public void ReportOwnedStart(IRigidbodyMaster master, bool releaseFromPreviousMaster = true)
        {
            if (master == null)
            {
                Debug.LogError(
                    $"ReportOwnedStart called with target master as null but that is not supported. "
                    + $"On {transform.GetPath()}", this);
                return;
            }

            if (releaseFromPreviousMaster)
                ReleaseFromMaster();

            if (this.master != null)
            {
                Debug.LogErrorFormat(this, "Object {0} is already owned by {1} while trying to"
                                           + " set new owner {2}", this, this.master, master);
            }

            isOwned = true;

            // Now cancel out its pre-existing velocity.
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;

            OnStartOwnedEvent?.Invoke(master);

            this.master = master;
            if (master is IRigidbodyMasterEventDispatcher slaveProvider)
            {
                slaveProvider.Slaves.Add(this);
                slaveProvider.ReportSlaveAdded(this);
            }
        }

        public void ReportOwnedStop(IRigidbodyMaster master)
        {
            if (this.master != master)
            {
                Debug.LogError(
                    $"ReportOwnedStop called with master {master} but it is not the master of this rigidbody"
                    + $", this is owned by {this.master}. On {transform.GetPath()}", this);
                return;
            }

            isOwned = false;
            OnStopOwnedEvent?.Invoke(master);

            if (master != null)
            {
                if (master is IRigidbodyMasterEventDispatcher slaveProvider)
                {
                    slaveProvider.Slaves.Remove(this);
                    slaveProvider.ReportSlaveRemoved(this);
                }
            }

            this.master = null;
        }

        public void ReleaseFromMaster()
        {
            if (master == null)
                return;

            IRigidbodyMaster beforeMaster = master;
            master.Release(this);
            Assert.AreNotEqual(master, beforeMaster);
        }

        public static implicit operator Rigidbody(RigidbodyWrapper rigidbodyWrapper)
        {
            if (rigidbodyWrapper == null)
                return null;
            return rigidbodyWrapper.Rigidbody;
        }

        public void ApplyCustomVelocity(
            Vector3 velocity,
            Vector3 angularVelocity = new Vector3()
        )
        {
            // Don't inverse the vectors here, should be done from where the override comes
            Velocity = velocity;

            AngularVelocity = angularVelocity;
        }

        private void OnCollisionEnter(Collision other)
        {
            OnCollisionEnterInternal(other, CollisionSource.Self);
        }

        public void ReportSubRigidbodyCollisionEnter(Collision other)
        {
            OnCollisionEnterInternal(other, CollisionSource.SubRigidbody);
        }

        private void OnCollisionEnterInternal(Collision other, CollisionSource source)
        {
            OnCollisionEnterEvent?.Invoke(other);
        }

        public Bounds GetWorldColliderBounds()
        {
            IList<Collider> colliders = cachedColliders ?? Colliders;
            Bounds result = default;
            bool hasFirstBounds = false;
            for (int i = colliders.Count - 1; i >= 0; i--)
            {
                Collider collider = colliders[i];

                if (collider == null)
                {
                    colliders.RemoveAt(i);
                    continue;
                }

                if (!collider.enabled)
                    continue;

                if (!hasFirstBounds)
                {
                    result = collider.bounds;
                    hasFirstBounds = true;
                }
                else
                {
                    result.Encapsulate(collider.bounds);
                }
            }
            return result;
        }

        
        public Vector3 GetCenter()
        {
            return transform.TransformPoint(cachedLocalColliderBounds.center);
        }

        
        public Vector3 GetLocalCenter()
        {
            return cachedLocalColliderBounds.center;
        }

        
        public Vector3 GetTop()
        {
            Vector3 center = cachedLocalColliderBounds.center;
            Vector3 extents = cachedLocalColliderBounds.extents;
            return transform.TransformPoint(center.x, center.y + extents.y, center.z);
        }

        
        public Vector3 GetBase()
        {
            Vector3 center = cachedLocalColliderBounds.center;
            Vector3 extents = cachedLocalColliderBounds.extents;
            return transform.TransformPoint(center.x, center.y - extents.y, center.z);
        }

        public void SetConstraints(RigidbodyConstraints constraints)
        {
            Rigidbody.constraints = constraints;
        }

        public void SetInterpolation(RigidbodyInterpolation interpolation)
        {
            Rigidbody.interpolation = interpolation;
            OnInterpolationChanged?.Invoke(this, interpolation);
        }

        public void SetMaxSpeed(float maxAcceleration, float maxSpeed)
        {
            if (!maxSpeedIsOverridden)
            {
                maxSpeedOriginalDrag = Drag;
                maxSpeedIsOverridden = true;
            }

            // Enforces the max speed via drag.
            // Source: https://forum.unity.com/threads/terminal-velocity.34667/
            float idealDrag = maxAcceleration / maxSpeed;
            Rigidbody.drag = idealDrag / ( idealDrag * Time.fixedDeltaTime + 1 );
        }

        public void ResetMaxSpeed()
        {
            if (!maxSpeedIsOverridden)
                return;

            maxSpeedIsOverridden = false;

            Drag = maxSpeedOriginalDrag;
        }

        public void OnRigidBodyCollisionEnter(RigidbodyWrapper rigidbodyWrapper, Collision other)
        {
            OnCollisionEnterInternal(other, CollisionSource.Listening);
        }

        public void SetForceContinuousDynamicCollisions(bool forceContinuousDynamicCollisions)
        {
            this.forceContinuousDynamicCollisions = forceContinuousDynamicCollisions;
        }

        public void SetDetectCollisions(bool shouldDetectCollisions)
        {
            Rigidbody.detectCollisions = shouldDetectCollisions;
        }

        public void IgnoreCollision(Rigidbody other, bool ignore = true)
        {
            RigidbodyWrapper rigidbodyWrapper = physicsService.Reference.GetWrapperComponent(other);
            IgnoreCollision(rigidbodyWrapper, ignore);
        }

        public void IgnoreCollision(RigidbodyWrapper other, bool ignore = true)
        {
            for (int i = 0; i < other.Colliders.Count; i++)
            {
                IgnoreCollision(other.Colliders[i], ignore);
            }
        }

        public void IgnoreCollision(Collider collider, bool ignore = true)
        {
            for (int i = 0; i < Colliders.Count; i++)
            {
                Physics.IgnoreCollision(Colliders[i], collider, ignore);
            }
        }

        public void SetCollidersEnabled(bool enabled, bool includeChildren = false)
        {
            collidersEnabled = enabled;

            IList<Collider> colliders = Colliders;
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider collider = colliders[i];
                collider.enabled = enabled;
            }

            if (!includeChildren)
                return;

            if (children == null)
                return;

            for (int i = 0; i < children.Count; i++)
            {
                children[i].RigidbodyWrapper.SetCollidersEnabled(enabled, true);
            }
        }

        public void ResetConstraints()
        {
            Rigidbody.constraints = originalConstraints;
        }

        public void ResetInterpolation()
        {
            Rigidbody.interpolation = originalInterpolation;
            OnInterpolationChanged?.Invoke(this, originalInterpolation);
        }

        public void SetPhysicalMaterial(PhysicMaterial physicMaterial)
        {
            IList<Collider> colliders = Colliders;
            for (int i = 0; i < colliders.Count; i++)
            {
                colliders[i].sharedMaterial = physicMaterial;
            }
        }

        public void ResetPhysicalMaterials()
        {
            IList<Collider> colliders = Colliders;
            for (int i = 0; i < colliders.Count; i++)
            {
                colliders[i].sharedMaterial = cachedPhysicMaterials[i];
            }
        }

        public void RegisterChild(ReparentableRigidbody child)
        {
            children.Add(child);
        }

        public void UnregisterChild(ReparentableRigidbody child)
        {
            if (children == null)
                return;

            children.Remove(child);
        }

        public Vector3 GetPointVelocity(Vector3 point)
        {
            return Rigidbody.GetPointVelocity(point);
        }

        public void SetUseGravity(bool useGravity)
        {
            this.Rigidbody.useGravity = useGravity;
        }
        
        public bool IsSleeping()
        {
            return Rigidbody.IsSleeping();
        }

        public void WakeUp()
        {
            Rigidbody.WakeUp();

            //Adding a small force to make sure the object Awake in the same frame
            AddForce(0.0000001f, 0, 0, ForceMode.Impulse);
        }

        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            Rigidbody.AddForce(force, mode);
        }

        public void AddForce(float x, float y, float z, ForceMode mode = ForceMode.Force)
        {
            AddForce(new Vector3(x, y, z), mode);
        }

        public void AddRelativeForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            Rigidbody.AddRelativeForce(force, mode);
        }

        public void AddRelativeForce(float x, float y, float z, ForceMode mode = ForceMode.Force)
        {
            AddRelativeForce(new Vector3(x, y, z), mode);
        }

        public void Accelerate(
            Vector3 acceleration, float maxSpeed, ForceMode mode = ForceMode.Acceleration,
            bool decelerateIfNecessary = false)
        {
            Vector3 currentVelocity = Rigidbody.velocity;

            Vector3 clampedAcceleration =
                CalculateClampedAcceleration(acceleration, currentVelocity, maxSpeed, mode, decelerateIfNecessary);

            if (clampedAcceleration == Vector3.zero)
                return;

            // Apply the clamped acceleration.
            Rigidbody.AddForce(clampedAcceleration, mode);
        }

        private Vector3 CalculateClampedAcceleration(
            Vector3 acceleration, Vector3 currentVelocity, float maxSpeed, ForceMode mode = ForceMode.Acceleration,
            bool decelerateIfNecessary = false)
        {
            if (Time.fixedDeltaTime.Approximately(0.0f))
                return Vector3.zero;

            Vector3 accelerationOriginal = acceleration;

            float massFactor = mode == ForceMode.Acceleration || mode == ForceMode.VelocityChange
                ? 1
                : Rigidbody.mass;

            Vector3 newVelocity;
            bool isInstant = mode == ForceMode.Impulse || mode == ForceMode.VelocityChange;

            // Calculate what the new velocity would be after the acceleration.
            if (isInstant)
                newVelocity = currentVelocity + acceleration / massFactor;
            else
                newVelocity = currentVelocity + acceleration / massFactor * Time.fixedDeltaTime;

            // Clamp that new velocity to our allowed max speed.
            newVelocity = Vector3.ClampMagnitude(newVelocity, maxSpeed);

            // Now calculate what the acceleration would be to reach that clamped new velocity.
            if (isInstant)
                acceleration = (newVelocity - currentVelocity) * massFactor;
            else
                acceleration = (newVelocity - currentVelocity) * massFactor / Time.fixedDeltaTime;

            // Disallow deceleration if necessary.
            if (!decelerateIfNecessary && Vector3.Dot(acceleration, accelerationOriginal) <= 0)
                return Vector3.zero;

            return acceleration;
        }

        public void Decelerate(float deceleration, Vector3 applyAxis, ForceMode mode = ForceMode.Acceleration)
        {
            Vector3 velocity = Rigidbody.velocity;
            velocity.Scale(applyAxis);

            float speed = velocity.magnitude;

            if (deceleration.Approximately(0.0f) || speed.Approximately(0.0f))
                return;

            deceleration = Mathf.Min(Mathf.Abs(speed / Time.fixedDeltaTime), deceleration);

            Vector3 direction = -velocity / speed;

            direction.Scale(applyAxis);

            AddForce(direction * deceleration, mode);
        }

        public void DecelerateAngular(float deceleration, Vector3 applyAxis, ForceMode mode = ForceMode.Acceleration)
        {
            Vector3 velocity = Rigidbody.angularVelocity;
            velocity.Scale(applyAxis);

            float speed = velocity.magnitude;

            if (deceleration.Approximately(0.0f) || speed.Approximately(0.0f))
                return;

            deceleration = Mathf.Min(Mathf.Abs(speed / Time.fixedDeltaTime), deceleration);

            Vector3 direction = -velocity / speed;

            direction.Scale(applyAxis);

            AddTorque(direction * deceleration, mode);
        }

        public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
        {
            Rigidbody.AddTorque(torque, mode);
        }

        public void AddTorque(float x, float y, float z, ForceMode mode = ForceMode.Force)
        {
            AddTorque(new Vector3(x, y, z), mode);
        }

        public void AddRelativeTorque(Vector3 torque, ForceMode mode)
        {
            Rigidbody.AddRelativeTorque(torque, mode);
        }

        public void AddRelativeTorque(Vector3 torque)
        {
            AddRelativeTorque(torque.x, torque.y, torque.z);
        }

        public void AddRelativeTorque(float x, float y, float z, ForceMode mode = ForceMode.Force)
        {
            AddRelativeTorque(new Vector3(x, y, z), mode);
        }

        public void AddForceAtPosition(
            Vector3 force, Vector3 position, ForceMode mode = ForceMode.Force)
        {
            Rigidbody.AddForceAtPosition(force, position, mode);
        }

        public void AccelerateAtPosition(
            Vector3 acceleration, Vector3 position, float maxSpeed, ForceMode mode = ForceMode.Acceleration,
            bool decelerateIfNecessary = false)
        {
            Vector3 currentVelocity = GetPointVelocity(position);

            Vector3 clampedAcceleration =
                CalculateClampedAcceleration(acceleration, currentVelocity, maxSpeed, mode, decelerateIfNecessary);

            if (clampedAcceleration == Vector3.zero)
                return;

            // Apply the clamped acceleration.
            Rigidbody.AddForceAtPosition(clampedAcceleration, position, mode);
        }

        public void AddExplosionForce(
            float explosionForce, Vector3 explosionPosition,
            float explosionRadius, float upwardsModifier = 0.0f,
            ForceMode mode = ForceMode.Force)
        {
            Rigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier,
                mode);
        }

        public void MovePosition(Vector3 position)
        {
            Rigidbody.MovePosition(position);
        }

        public void MovePositionLocal(Vector3 localPosition)
        {
            Vector3 worldPosition;
            if (transform.parent.IsNull())
                worldPosition = localPosition;
            else
                worldPosition = transform.parent.TransformPoint(localPosition);

            Rigidbody.MovePosition(worldPosition);
        }

        public void MoveRotation(Quaternion rotation)
        {
            Rigidbody.MoveRotation(rotation);
        }
    }
}