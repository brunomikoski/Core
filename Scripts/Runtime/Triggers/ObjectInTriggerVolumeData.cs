using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.PhysX
{
    [Serializable]
    public struct ObjectInTriggerVolumeData : IDisposable, IEquatable<ObjectInTriggerVolumeData>
    {
        [SerializeField]
        private Rigidbody rigidbody;

        public List<Collider> collidersInVolume;

        private PoolList<Collider> poolList;

        public ObjectInTriggerVolumeData(Rigidbody rigidbody)
        {
            this.rigidbody = rigidbody;
            poolList = PoolList<Collider>.ObtainLongTerm();
            collidersInVolume = poolList;
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
            if (poolList != null)
                poolList.Dispose();
            poolList = null;
        }

    }
}
