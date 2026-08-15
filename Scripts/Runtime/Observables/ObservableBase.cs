using System;

namespace BrunoMikoski.Core.Observables {

    [Serializable]
    public abstract class ObservableBase {
        public abstract event Action OnChangedEvent;
        public abstract void RaiseOnChangedEvent();

        public abstract void OnValidate();

        protected abstract string ValuePropName { get; }

        protected abstract void OnBeginGui();

        public abstract void ResetToInitialValue();
    }
}
