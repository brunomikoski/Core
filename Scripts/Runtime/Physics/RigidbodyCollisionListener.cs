using System;
using BrunoMikoski.Core;
using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    public class RigidbodyCollisionListener
    {
        private readonly Action<Collision> onEnter;
        private readonly Action<Collision> onExit;

        public RigidbodyCollisionListener(Action<Collision> onEnter = null, Action<Collision> onExit = null)
        {
            this.onEnter = onEnter;
            this.onExit = onExit;
        }

        public void OnRigidBodyCollisionEnter(RigidbodyWrapper rigidbodyWrapper, Collision other)
        {
            onEnter?.Invoke(other);
        }

        public void OnRigidBodyCollisionExit(RigidbodyWrapper rigidbodyWrapper, Collision other)
        {
            onExit?.Invoke(other);
        }
    }
}