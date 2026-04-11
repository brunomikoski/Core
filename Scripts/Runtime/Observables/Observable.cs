using System;
using System.Collections.Generic;
using UnityEngine;
using BrunoMikoski.Core.Events;

namespace BrunoMikoski.Core.Observables {

    [Serializable]
    public class ObservableGameObject : Observable<GameObject> { }

    [Serializable]
    public class ObservableTransform : Observable<Transform> { }

    [Serializable]
    public class ObservableVector3 : Observable<Vector3> { }

    [Serializable]
    public class ObservableBool : Observable<bool> { }

    [Serializable]
    public class ObservableInt : Observable<int> { }

    [Serializable]
    public class ObservableFloat : Observable<float> { }

    [Serializable]
    public class Observable<T> : ObservableBase, IEquatable<Observable<T>> {

        [SerializeField]
        private T initializationValue;

        [SerializeField]
        private T value;

        private T prevValue;
        private bool prevValueInitialized = false;

        [NonSerialized]
        private SafeEvent _onChangedEvent = new();

        public override event Action OnChangedEvent
        {
            add => _onChangedEvent.AddListener(value);
            remove => _onChangedEvent.RemoveListener(value);
        }

        [NonSerialized]
        private SafeEvent<T, T> _onChangedValuesEvent = new();
        public event Action<T, T> OnChangedValuesEvent
        {
            add => _onChangedValuesEvent.AddListener(value);
            remove => _onChangedValuesEvent.RemoveListener(value);
        }

        protected override string ValuePropName => "value";

        public Observable() {
            initializationValue = value;
        }

        public Observable(T value) {
            this.value = value;
            initializationValue = value;
        }

        public T Value {
            get => value;
            set => SetValue(value);
        }

        public void SetValue(T targetValue, bool notify = true)
        {
            if (EqualityComparer<T>.Default.Equals(value, targetValue)) {
                return;
            }

            T previous = targetValue;
            prevValue = previous;
            prevValueInitialized = true;

            value = targetValue;

            if (notify)
            {
                _onChangedEvent.Dispatch();
                _onChangedValuesEvent.Invoke(previous, value);
            }
        }

        public static explicit operator T(Observable<T> observable) {
            return observable.value;
        }

        public override string ToString() {
            return value != null ? value.ToString() : "null";
        }

        public bool Equals(Observable<T> other) {
            if (other == null) {
                return false;
            }
            return EqualityComparer<T>.Default.Equals(other.value, value);
        }

        public override bool Equals(object other) {
            return other is Observable<T> o && EqualityComparer<T>.Default.Equals(o.value, value);
        }

        public override int GetHashCode() {
            return value != null ? value.GetHashCode() : 0;
        }

        protected override void OnBeginGui() {
            prevValue = value;
            prevValueInitialized = true;
        }

        public override void OnValidate() {
            if (prevValueInitialized) {
                var nextValue = value;
                value = prevValue;
                Value = nextValue;
            } else {
                prevValue = value;
                prevValueInitialized = true;
            }
        }

        public override void ResetToInitialValue() {
            Value = initializationValue;
        }
    }
}
