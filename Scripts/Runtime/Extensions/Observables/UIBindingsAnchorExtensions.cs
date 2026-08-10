using UnityEngine;

namespace BrunoMikoski.Core.Utils.Extensions.Observables
{
    public static class UIBindingsAnchorExtensions
    {
        public static UIBinding AddTo(this UIBinding binding, Component owner)
        {
            return binding.AddTo(owner.gameObject);
        }

        public static UIBinding AddTo(this UIBinding binding, GameObject owner)
        {
            if (!owner.TryGetComponent(out UIBindingsAnchor anchor))
            {
                anchor = owner.AddComponent<UIBindingsAnchor>();
            }

            anchor.Add(binding);
            return binding;
        }

        public static void DisposeBindings(this Component owner)
        {
            owner.gameObject.DisposeBindings();
        }

        public static void DisposeBindings(this GameObject owner)
        {
            if (owner.TryGetComponent(out UIBindingsAnchor anchor))
                anchor.DisposeAll();
        }
    }
}
