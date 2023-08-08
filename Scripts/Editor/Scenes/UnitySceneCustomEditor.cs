using System;
using System.Collections.Generic;
using System.Linq;
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
            }
            
            BuildState buildState = GetBuildStateForSceneAtPath(AssetDatabase.GUIDToAssetPath(guidProperty.stringValue));
            
            EditorGUILayout.BeginHorizontal();

            // Use a control ID for the PrefixLabel, so that following controls can align with it
            EditorGUILayout.PrefixLabel(new GUIContent("Scene"));

            //  Displaying custom icon for build state
            string unityEditorIconName = buildState == BuildState.InBuildSettings ? "greenLight" : "d_redLight";
            // GUILayout.Label(EditorGUIUtility.IconContent(unityEditorIconName),  GUILayout.Height(20),GUILayout.Width(20));
            GUILayout.Label(EditorGUIUtility.IconContent(unityEditorIconName), GUILayout.MaxHeight(20), GUILayout.Width(20), GUILayout.ExpandWidth(false));

            // Scene field  
            EditorGUI.BeginChangeCheck();
            SceneAsset newSceneAsset = EditorGUILayout.ObjectField(sceneAsset, typeof(SceneAsset), false) as SceneAsset;


            // Cog button
            if (GUILayout.Button(EditorGUIUtility.IconContent("_Popup@2x"), GUILayout.Width(20)))
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

                menu.AddItem(new GUIContent("Show at the scene picker"), ScenePickerToolbar.IsShowingAtScenePicker(scenePathProperty.stringValue), () =>
                {
                    if (ScenePickerToolbar.IsShowingAtScenePicker(scenePathProperty.stringValue))
                        ScenePickerToolbar.HideFromScenePicker(scenePathProperty.stringValue);
                    else
                        ScenePickerToolbar.ShowAtScenePicker(scenePathProperty.stringValue);
                });

                menu.ShowAsContext();
            }

            EditorGUILayout.EndHorizontal();
            
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
