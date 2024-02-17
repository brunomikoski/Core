using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BrunoMikoski.Core.PhysX
{
    public struct ObjectInTriggerVolumeData : IDisposable, IEquatable<ObjectInTriggerVolumeData>
    {
        [SerializeField]
        private Rigidbody rigidbody;

        public List<Collider> collidersInVolume;

        public ObjectInTriggerVolumeData(Rigidbody rigidbody)
        {
            this.rigidbody = rigidbody;
            collidersInVolume = ListPool<Collider>.Get();
        }

        public static implicit operator ObjectInTriggerVolumeData(Rigidbody rigidbody)
        {
            return new ObjectInTriggerVolumeData()
            {
                rigidbody = rigidbody
            };
        }

        public static implicit operator Rigidbody(ObjectInTriggerVolumeData wrapper)
        {
            return wrapper.rigidbody;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectInTriggerVolumeData other && Equals(other);
        }

        public bool Equals(ObjectInTriggerVolumeData other)
        {
            return Equals(rigidbody, other.rigidbody);
        }

        public override int GetHashCode()
        {
            return (rigidbody != null ? rigidbody.GetHashCode() : 0);
        }

        public void Dispose()
        {
            if (collidersInVolume != null)
                ListPool<Collider>.Release(collidersInVolume);
            
            collidersInVolume = null;
        }

    }
}
