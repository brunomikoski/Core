using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BrunoMikoski.Core.Builds
{
    public class DOTweenPreBuildStep : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            bool isDevelopment = (report.summary.options & BuildOptions.Development) != 0;
            SetDebug(isDevelopment);
            Debug.Log(isDevelopment
                ? ":: DOTween debug logging ENABLED for development build"
                : ":: DOTween debug logging DISABLED for release build");
        }

        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
        {
            // Restore the debug-on default so we keep target-id logging while working in-editor
            // (a non-release context). Matches FusionPreBuildStep restoring the shared DLLs to debug.
            SetDebug(true);
        }

        private static void SetDebug(bool enabled)
        {
            string[] guids = AssetDatabase.FindAssets("t:DOTweenSettings");
            if (guids.Length == 0)
            {
                Debug.LogWarning(":: DOTweenPreBuildStep could not find a DOTweenSettings asset; skipping.");
                return;
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (settings == null)
                    continue;

                SerializedObject so = new SerializedObject(settings);

                bool changed = false;
                changed |= TrySetBool(so, "debugMode", enabled);
                changed |= TrySetBool(so, "debugStoreTargetId", enabled);
                // Target-id capture only works while safe mode is on, so guarantee it for dev builds.
                if (enabled)
                    changed |= TrySetBool(so, "useSafeMode", true);

                if (!changed)
                    continue;

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();
        }

        private static bool TrySetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($":: DOTweenPreBuildStep: property '{propertyName}' not found on DOTweenSettings; the DOTween version may have changed.");
                return false;
            }

            if (property.boolValue == value)
                return false;

            property.boolValue = value;
            return true;
        }
    }
}
