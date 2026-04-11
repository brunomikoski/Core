using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.ScenesReferences
{
    [CustomEditor(typeof(UnityScene), true)]
    public class UnitySceneCustomEditor : UnityEditor.Editor
    {
        private SceneAsset sceneAsset;

        private enum BuildState
        {
            InBuildSettings,
            NotInBuildSettings
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            serializedObject.Update();

            SerializedProperty guidProperty = serializedObject.FindProperty("sceneGuid");
            SerializedProperty scenePathProperty = serializedObject.FindProperty("scenePath");
            SerializedProperty buildIndexProperty = serializedObject.FindProperty("buildIndex");

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

                if (string.IsNullOrEmpty(scenePathProperty.stringValue))
                {
                    scenePathProperty.stringValue = path;
                    serializedObject.ApplyModifiedProperties();
                }

                int currentBuildIndex = GetBuildIndexForSceneAtPath(path);
                if (buildIndexProperty.intValue != currentBuildIndex)
                {
                    buildIndexProperty.intValue = currentBuildIndex;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            BuildState buildState = GetBuildStateForSceneAtPath(AssetDatabase.GUIDToAssetPath(guidProperty.stringValue));

            EditorGUILayout.BeginHorizontal();


            float toolboxButtonWidth = EditorGUIUtility.singleLineHeight;
            Rect rect = EditorGUILayout.GetControlRect();
            Rect selectorFieldRect = EditorGUI.PrefixLabel(rect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("Scene"));
            selectorFieldRect.x -= 14;
            selectorFieldRect.height = EditorGUIUtility.singleLineHeight;
            selectorFieldRect.width -= toolboxButtonWidth - 14;

            string unityEditorIconName = buildState == BuildState.InBuildSettings ? "greenLight" : "d_redLight";
            GUIContent statusIcon = EditorGUIUtility.IconContent(unityEditorIconName);

            Rect statusButtonRect = new Rect()
            {
                x = selectorFieldRect.x-4,
                width = toolboxButtonWidth,
                y = selectorFieldRect.y + 1,
                height = selectorFieldRect.height - 1
            };

            GUI.Label(statusButtonRect, statusIcon);

            SceneAsset newSceneAsset = (SceneAsset)EditorGUI.ObjectField(selectorFieldRect, sceneAsset, typeof(SceneAsset), false);

            Rect toolboxButtonRect = new Rect
            {
                x = selectorFieldRect.x + selectorFieldRect.width + 2,
                width = toolboxButtonWidth,
                y = selectorFieldRect.y + 1,
                height = selectorFieldRect.height - 1
            };

            GUIContent settingsIcon = EditorGUIUtility.IconContent("SettingsIcon");
            if (GUI.Button(toolboxButtonRect, settingsIcon, EditorStyles.iconButton))
            {
                GenericMenu menu = new GenericMenu();
                if(buildState == BuildState.NotInBuildSettings)
                {
                    menu.AddItem(new GUIContent("Add to Build Settings"), false, () =>
                    {
                        AddSceneToBuildSettings(scenePathProperty.stringValue);
                        serializedObject.FindProperty("buildIndex").intValue =
                            GetBuildIndexForSceneAtPath(scenePathProperty.stringValue);
                    });

                }

                if(buildState == BuildState.InBuildSettings)
                {
                    menu.AddItem(new GUIContent("Remove from Build Settings"), false, () =>
                    {
                        RemoveSceneFromBuildSettings(scenePathProperty.stringValue);
                        serializedObject.FindProperty("buildIndex").intValue = -1;
                    });
                }

                menu.ShowAsContext();

            }

            EditorGUILayout.EndHorizontal();

            SerializedProperty sceneNameProperty = serializedObject.FindProperty("sceneName");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Build Index", buildIndexProperty.intValue);
            EditorGUILayout.TextField("Scene Name", sceneNameProperty.stringValue);
            EditorGUILayout.TextField("Scene Path", scenePathProperty.stringValue);
            EditorGUI.EndDisabledGroup();

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
                        scenePathProperty.stringValue = path;
                        serializedObject.FindProperty("buildIndex").intValue =
                            GetBuildIndexForSceneAtPath(scenePathProperty.stringValue);
                    }
                    else
                    {
                        guidProperty.stringValue = string.Empty;
                        serializedObject.FindProperty("sceneName").stringValue = string.Empty;
                        scenePathProperty.stringValue = string.Empty;
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


        private BuildState GetBuildStateForSceneAtPath(string targetPath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; ++i)
            {
                EditorBuildSettingsScene entry = scenes[i];
                if (entry.path.Equals(targetPath, StringComparison.Ordinal))
                    return BuildState.InBuildSettings;
            }

            return BuildState.NotInBuildSettings;
        }

        private void AddSceneToBuildSettings(string targetPath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = 0; i < scenes.Count; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (string.Equals(scene.path, targetPath, StringComparison.Ordinal))
                    return;
            }

            EditorBuildSettingsScene newScene = new EditorBuildSettingsScene
            {
                path = targetPath,
                enabled = true
            };

            scenes.Add(newScene);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private void RemoveSceneFromBuildSettings(string targetPath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool changed = false;
            for (int i = scenes.Count - 1; i >= 0; i--)
            {
                EditorBuildSettingsScene scene = scenes[i];
                if (string.Equals(scene.path, targetPath, StringComparison.Ordinal))
                {

                    scenes.RemoveAt(i);
                    changed = true;
                    break;
                }
            }

            if (changed)
                EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
