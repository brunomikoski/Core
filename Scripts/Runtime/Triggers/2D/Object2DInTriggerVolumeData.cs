using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BrunoMikoski.Core.DevelopmentTools.PhysX
{
    [Serializable]
    public struct Object2DInTriggerVolumeData : IDisposable, IEquatable<Object2DInTriggerVolumeData>

    {
        [SerializeField] 
        private Rigidbody2D rigidbody;

        public List<Collider2D> collidersInVolume;

        public Object2DInTriggerVolumeData(Rigidbody2D rigidbody)
        {
            this.rigidbody = rigidbody;
            collidersInVolume = ListPool<Collider2D>.Get();
        }

        public static implicit operator Object2DInTriggerVolumeData(Rigidbody2D rigidbody)
        {
            return new Object2DInTriggerVolumeData()
            {
                rigidbody = rigidbody
            };
        }

        public static implicit operator Rigidbody2D(Object2DInTriggerVolumeData wrapper)
        {
            return wrapper.rigidbody;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectInTriggerVolumeData other && Equals(other);
        }

        public bool Equals(Object2DInTriggerVolumeData other)
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
                ListPool<Collider2D>.Release(collidersInVolume);

            collidersInVolume = null;
        }
    }
}