using System.Collections.Generic;
using BrunoMikoski.ServicesLocation;
using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
#if RIGIDBODYWRAPPER_ENABLED
    [ServiceImplementation(DependsOn = new []{typeof(MonoBehaviourService)})]
#endif
    public sealed class PhysicsService : IFixedUpdate
    {
        private readonly Dictionary<Rigidbody, RigidbodyWrapper> rigidbodyToWrapper = new();

        private List<RigidbodyWrapper> rigidbodyWrappers = new();

        private readonly Dictionary<Collider, RigidbodyWrapper> colliderToWrapper = new();

        public RigidbodyWrapper GetWrapperComponent(Rigidbody rigidbody)
        {
            if (rigidbody == null)
                return null;

            RigidbodyWrapper result;

            rigidbodyToWrapper.TryGetValue(rigidbody, out result);
            return result;
        }

        public RigidbodyWrapper GetWrapperComponent(Collider collider, bool includeTriggers = false)
        {
            if (collider == null)
                return null;

            colliderToWrapper.TryGetValue(collider, out RigidbodyWrapper result);

            if (result != null)
                return result;


            if (collider.attachedRigidbody == null)
                return null;

            rigidbodyToWrapper.TryGetValue(collider.attachedRigidbody, out result);
            if (result != null)
            {
                if (includeTriggers && collider.isTrigger)
                    return result;

                if (!result.Colliders.Contains(collider))
                {
                    for (int i = 0; i < result.Children.Count; i++)
                    {
                        ReparentableRigidbody reparentableRigidbody = result.Children[i];

                        if (reparentableRigidbody.RigidbodyWrapper.Colliders.Contains(collider))
                        {
                            result = reparentableRigidbody.RigidbodyWrapper;
                            RegisterColliderToWrapper(collider, result);
                            return result;
                        }
                    }
                    return null;
                }
            }
            return result;
        }

        public void RegisterPhysicsObject(RigidbodyWrapper rigidbodyWrapper)
        {
            rigidbodyWrappers.Add(rigidbodyWrapper);
            rigidbodyToWrapper[rigidbodyWrapper.Rigidbody] = rigidbodyWrapper;

            for (int i = 0; i < rigidbodyWrapper.Colliders.Count; i++)
                RegisterColliderToWrapper(rigidbodyWrapper.Colliders[i], rigidbodyWrapper);
        }

        public void RegisterSubPhysicsObject(RigidbodyWrapper rigidbodyWrapper, Rigidbody rigidbody)
        {
            rigidbodyToWrapper[rigidbody] = rigidbodyWrapper;
        }

        private void RegisterColliderToWrapper(Collider collider, RigidbodyWrapper rigidbodyWrapper)
        {
            if (collider == null)
            {
                Debug.LogWarning($"Found null collider on rigidbody wrapper: {rigidbodyWrapper.gameObject.name}.");
                return;
            }

            colliderToWrapper[collider] = rigidbodyWrapper;
        }

        public void UnregisterPhysicsObject(RigidbodyWrapper rigidbodyWrapper)
        {
            rigidbodyWrappers.Remove(rigidbodyWrapper);
            rigidbodyToWrapper.Remove(rigidbodyWrapper.Rigidbody);

            for (int i = 0; i < rigidbodyWrapper.Colliders.Count; i++)
                UnregisterCollider(rigidbodyWrapper.Colliders[i]);
        }

        public void UnregisterSubPhysicsObject(Rigidbody rigidbody)
        {
            rigidbodyToWrapper.Remove(rigidbody);
        }

        public void UnregisterCollider(Collider collider)
        {
            colliderToWrapper.Remove(collider);
        }

        public void OnFixedUpdate()
        {
            for (int i = 0; i < rigidbodyWrappers.Count; i++)
                rigidbodyWrappers[i].PhysicsUpdate();
        }
    }
}
