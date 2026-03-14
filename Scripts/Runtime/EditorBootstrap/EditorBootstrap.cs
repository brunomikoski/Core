using System;

namespace BrunoMikoski.Core
{
    public static class EditorBootstrap
    {
        private const string EDITOR_BOOT_SKIP_KEY = "EDITOR_SCENE_SKIP";

        public static bool IsSkippingScenes
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetBool(EDITOR_BOOT_SKIP_KEY, false);
#endif
#pragma warning disable CS0162
                return false;
#pragma warning restore CS0162
            }
        }

        public static Action OnEditorBootstrapCompleteEvent;

        public static void SetBackToTheNormalFlow()
        {
#if UNITY_EDITOR
            UnityEditor.SessionState.SetBool(EDITOR_BOOT_SKIP_KEY, false);
#endif           
        }
    }
}
