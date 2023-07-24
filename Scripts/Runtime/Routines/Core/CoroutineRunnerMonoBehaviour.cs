using UnityEngine;

namespace BrunoMikoski.Core.Routines
{
    [DefaultExecutionOrder(-10000)]
    public class CoroutineRunnerMonoBehaviour : MonoBehaviour
    {
        private void OnApplicationQuit()
        {
            CoroutineRunner.OnApplicationQuit();
        }

        private void OnDestroy()
        {
            CoroutineRunner.OnRunnerDestroyed();

            CoroutineRunner.StopAll();
        }
    }
}
