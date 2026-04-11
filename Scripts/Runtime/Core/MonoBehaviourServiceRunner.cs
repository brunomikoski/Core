using UnityEngine;

namespace BrunoMikoski.Core
{
    public class MonoBehaviourServiceRunner : MonoBehaviour {
        private MonoBehaviourService _service;
        public void Initialize(MonoBehaviourService monoBehaviourService) {
            _service = monoBehaviourService;
        }

        private void OnApplicationQuit()
        {
            _service?.OnApplicationQuit();
        }

        private void Update() {
            _service.Update();
        }
        private void FixedUpdate() {
            _service.FixedUpdate();
        }

        private void LateUpdate() {
            _service.LateUpdate();
        }
    }
}
