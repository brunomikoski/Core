using System;
using System.Collections.Generic;

namespace BrunoMikoski.Core.Observables {

    /**
     * A convenient way to bind a single event handler to multiple observables at once. If any of the observables are
     * changed then the action will be invoked.
     */
    public class ObservableEffect : IDisposable {

        public static ObservableEffect Create(Action action, params ObservableBase[] obs) => Create(action, obs, true);
        public static ObservableEffect CreateNoFireImmediate(Action action, params ObservableBase[] obs) => Create(action, obs, false);
        public static ObservableEffect Create(Action action, IEnumerable<ObservableBase> obs, bool fireImmediate = true) {
            var instance = new ObservableEffect(action, obs);
            if (fireImmediate) {
                action?.Invoke();
            }
            return instance;
        }

        readonly List<ObservableBase> observables = new List<ObservableBase>();
        Action action;

        ObservableEffect() { }

        ObservableEffect(Action action, IEnumerable<ObservableBase> dependencies) {
            foreach (var o in dependencies) {
                if (o == null) continue;
                observables.Add(o);
                o.OnChangedEvent += FireEvent;
            }
            this.action = action;
        }

        public void Dispose() {
            foreach (var o in observables) {
                o.OnChangedEvent -= FireEvent;
            }
        }

        void FireEvent() {
            action?.Invoke();
        }
    }

}
