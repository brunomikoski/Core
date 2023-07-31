using System;
using UnityEditor;

namespace BrunoMikoski.Core.ScenesReferences
{
    [CustomEditor(typeof(UnityScene), true)]
    public class UnitySceneCustomEditor : UnityEditor.Editor
    {
        private SceneAsset sceneAsset;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            serializedObject.Update();
            
            SerializedProperty guidProperty = serializedObject.FindProperty("sceneGuid");

            if (!string.IsNullOrEmpty(guidProperty.stringValue))
            {
                string path = AssetDatabase.GUIDToAssetPath(guidProperty.stringValue);
                if (!string.IsNullOrEmpty(path))
                    sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

                if (string.IsNullOrEmpty(serializedObject.FindProperty("sceneName").stringValue))
                {
                    serializedObject.FindProperty("sceneName").stringValue = sceneAsset.name;
                    serializedObject.ApplyModifiedProperties();
                }

                if (string.IsNullOrEmpty(serializedObject.FindProperty("scenePath").stringValue))
                {
                    serializedObject.FindProperty("scenePath").stringValue = path;
                    serializedObject.ApplyModifiedProperties();
                }
            }
            
            EditorGUI.BeginChangeCheck();
            SceneAsset newSceneAsset =
                EditorGUILayout.ObjectField("Scene", sceneAsset, typeof(SceneAsset), false) as SceneAsset;

            if (EditorGUI.EndChangeCheck())
            {
                if (newSceneAsset != sceneAsset)
                {
                    sceneAsset = newSceneAsset;
                    if (sceneAsset != null)
                    {
                        string path = AssetDatabase.GetAssetPath(sceneAsset);
                        guidProperty.stringValue = AssetDatabase.AssetPathToGUID(path);
                        serializedObject.FindProperty("sceneName").stringValue = sceneAsset.name;
                        serializedObject.FindProperty("scenePath").stringValue = path;
                        serializedObject.FindProperty("buildIndex").intValue =
                            GetBuildIndexForSceneAtPath(guidProperty.stringValue);
                    }
                    else
                    {
                        guidProperty.stringValue = string.Empty;
                        serializedObject.FindProperty("sceneName").stringValue = string.Empty;
                        serializedObject.FindProperty("scenePath").stringValue = string.Empty;
                        serializedObject.FindProperty("buildIndex").intValue = -1;
                    }

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private int GetBuildIndexForSceneAtPath(string targetPath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; ++i)
            {
                EditorBuildSettingsScene entry = scenes[i];
                if (entry.path.Equals(targetPath, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }
}
