using System.Collections.Generic;
using UnityEngine;

namespace BrunoMikoski.Core.Utils.Extensions.Observables
{
    [DisallowMultipleComponent]
    public sealed class UIBindingsAnchor : MonoBehaviour
    {
        private readonly List<UIBinding> _bindings = new();

        public void Add(UIBinding binding)
        {
            _bindings.Add(binding);
        }

        public void DisposeAll()
        {
            foreach (UIBinding binding in _bindings)
            {
                binding.Dispose();
            }
            _bindings.Clear();
        }

        private void OnDestroy()
        {
            DisposeAll();
        }
    }
}
