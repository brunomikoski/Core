using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.Core.Utils;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BrunoMikoski.Core.ScenesReferences
{
    public class ScenePickerToolbar
    {
        public const string HIDE_FROM_SCENE_PICKER_KEY = "HIDE_FROM_SCENE_PICKER_KEY";

        private static UnityScenesCollection SCENES_COLLECTION;
        private static GUIContent SCENE_ICON;
        private static GUIStyle GUI_STYLE;

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            SCENE_ICON = EditorGUIUtility.IconContent("d_SceneAsset Icon");
            SCENE_ICON.text = "Scenes";
            
            EditorApplication.delayCall += Initialize;
        }
        
        private static void Initialize()
        {
            VisualElement parent = new VisualElement()
            {
                style =
                {
                    flexGrow = 0,
                    flexDirection = FlexDirection.Row,
                },
            };

            parent.Add(new VisualElement()
            {
                style =
                {
                    flexGrow = 1,
                },
            });

            parent.Add(new ToolbarSpacer());
            parent.Add(new IMGUIContainer(DropDownIMGUI));

            UnityMainToolbarUtility.AddCustom(UnityMainToolbarUtility.TargetContainer.Left,
                UnityMainToolbarUtility.Side.Right, parent);
        }
        
        public static bool IsHiddenFromScenePicker(string scenePath)
        {
            string hiddenPickerList = EditorPrefs.GetString(HIDE_FROM_SCENE_PICKER_KEY, "");
            string[] hiddenPickerListArray = hiddenPickerList.Split(",", StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < hiddenPickerListArray.Length; i++)
            {
                string hiddenPath = hiddenPickerListArray[i];
                if (hiddenPath.Equals(scenePath, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public static void HideFromScenePicker(string scenePath)
        {
            string hiddenPickerList = EditorPrefs.GetString(HIDE_FROM_SCENE_PICKER_KEY, "");
            string[] hiddenPickerListArray = hiddenPickerList.Split(",", StringSplitOptions.RemoveEmptyEntries);
            Array.Resize(ref hiddenPickerListArray, hiddenPickerListArray.Length + 1);
            hiddenPickerListArray[^1] = scenePath;
            EditorPrefs.SetString(HIDE_FROM_SCENE_PICKER_KEY, string.Join(",", hiddenPickerListArray));
        }
        
        public static void ShowAtScenePicker(string scenePath)
        {
            string hiddenPickerList = EditorPrefs.GetString(HIDE_FROM_SCENE_PICKER_KEY, "");
            List<string> hiddenPickerListArray = hiddenPickerList.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = hiddenPickerListArray.Count - 1; i >= 0; i--)
            {
                string ignoredScenePath = hiddenPickerListArray[i];
                if (ignoredScenePath.Equals(scenePath, StringComparison.Ordinal))
                {
                    hiddenPickerListArray.RemoveAt(i);
                    break;
                }
            }

            EditorPrefs.SetString(HIDE_FROM_SCENE_PICKER_KEY, string.Join(",", hiddenPickerListArray));
        }


        private static void DropDownIMGUI()
        {
            if (!CollectionsRegistry.Instance.TryGetCollectionOfType(out SCENES_COLLECTION))
                return;

            if (GUI_STYLE == null && EditorStyles.toolbarPopup != null)
            {
                GUI_STYLE = new GUIStyle(EditorStyles.toolbarPopup)
                {
                    fixedHeight = 19,
                    fixedWidth = 100,
                };
            }
            
            
            if (!EditorGUILayout.DropdownButton(SCENE_ICON, FocusType.Passive, GUI_STYLE))
            {
                return;
            }
 
            void HandleItemClicked(object parameter)
            {
                if (parameter is UnityScene unityScene)
                {
                    if (unityScene.IsLoaded())
                    {
                        if(CanUnload())
                            unityScene.Unload();
                    }
                    else
                    {
                        unityScene.Load();
                    }
                }
                else if (parameter is Scene scene)
                {
                    if (scene.isLoaded)
                    {
                        if (CanUnload())
                        {
                            if (EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new Scene[] { scene }))
                            {
                                EditorSceneManager.CloseScene(scene, true);
                            }
                        }
                        else
                        {
                            EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
                        }
                    }

                }
            }
 
            GenericMenu menu = new();
            for (int i = 0; i < SCENES_COLLECTION.Count; i++)
            {
                UnityScene unityScene = SCENES_COLLECTION[i];
                if (IsHiddenFromScenePicker(unityScene.ScenePath))
                    continue;
                
                menu.AddItem(new GUIContent(unityScene.name), unityScene.IsLoaded(), HandleItemClicked, unityScene);
            }

            bool addedSeparator = false;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene openScene = SceneManager.GetSceneAt(i);
                if (SCENES_COLLECTION.Any(scene => scene.Scene == openScene))
                    continue;

                if (IsHiddenFromScenePicker(openScene.path))
                    continue;

                if (!addedSeparator)
                {
                    menu.AddSeparator("");
                    addedSeparator = true;
                }

                string openSceneName = openScene.name;
                if (string.IsNullOrEmpty(openSceneName))
                {
                    openSceneName = $"Untitled Scene {i}";
                }

                menu.AddItem(new GUIContent(openSceneName), true, HandleItemClicked, openScene);
            }

            menu.DropDown(EditorGUILayout.GetControlRect());
        }

        private static bool CanUnload()
        {
            return SceneManager.sceneCount > 1;
        }
    }
}