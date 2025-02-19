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

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            SCENE_ICON = EditorGUIUtility.IconContent("d_SceneAsset Icon");
            SCENE_ICON.text = "Scenes";
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            VisualElement parent = new VisualElement
            {
                style =
                {
                    flexGrow = 0,
                    flexDirection = FlexDirection.Row,
                },
            };

            // Optional filler element to push our control to the right.
            parent.Add(new VisualElement { style = { flexGrow = 1 } });

            // Create a ToolbarMenu that will serve as our scene picker.
            ToolbarMenu scenePickerMenu = new ToolbarMenu
            {
                tooltip = "Click to see available scenes",
            };

            // Build a content container with an icon and label.
            VisualElement content = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                }
            };
            VisualElement iconElement = new VisualElement();
            iconElement.style.backgroundImage = new StyleBackground((Texture2D)SCENE_ICON.image);
            iconElement.style.width = 16;
            iconElement.style.height = 16;
            iconElement.style.marginRight = 4;
            content.Add(iconElement);
            Label label = new Label(SCENE_ICON.text);
            content.Add(label);
            scenePickerMenu.Add(content);

            ApplyToolbarStyle(scenePickerMenu);

            scenePickerMenu.RegisterCallback<MouseUpEvent>(e =>
            {
                if (e.button == 0)
                    ShowScenesMenu(scenePickerMenu);
            });
            parent.Add(scenePickerMenu);

            UnityMainToolbarUtility.AddCustom(UnityMainToolbarUtility.TargetContainer.Left,
                UnityMainToolbarUtility.Side.Right, parent);
        }

        public static bool IsHiddenFromScenePicker(string scenePath)
        {
            string hiddenPickerList = EditorPrefs.GetString(HIDE_FROM_SCENE_PICKER_KEY, "");
            string[] hiddenPickerListArray = hiddenPickerList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string hiddenPath in hiddenPickerListArray)
            {
                if (hiddenPath.Equals(scenePath, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        public static void HideFromScenePicker(string scenePath)
        {
            string hiddenPickerList = EditorPrefs.GetString(HIDE_FROM_SCENE_PICKER_KEY, "");
            string[] hiddenPickerListArray = hiddenPickerList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Array.Resize(ref hiddenPickerListArray, hiddenPickerListArray.Length + 1);
            hiddenPickerListArray[^1] = scenePath;
            EditorPrefs.SetString(HIDE_FROM_SCENE_PICKER_KEY, string.Join(",", hiddenPickerListArray));
        }

        public static void ShowAtScenePicker(string scenePath)
        {
            string hiddenPickerList = EditorPrefs.GetString(HIDE_FROM_SCENE_PICKER_KEY, "");
            List<string> hiddenPickerListArray = hiddenPickerList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (int i = hiddenPickerListArray.Count - 1; i >= 0; i--)
            {
                if (hiddenPickerListArray[i].Equals(scenePath, StringComparison.Ordinal))
                {
                    hiddenPickerListArray.RemoveAt(i);
                    break;
                }
            }
            EditorPrefs.SetString(HIDE_FROM_SCENE_PICKER_KEY, string.Join(",", hiddenPickerListArray));
        }

        private static void ShowScenesMenu(ToolbarMenu menu)
        {
            if (!CollectionsRegistry.Instance.TryGetCollectionOfType(out SCENES_COLLECTION))
                return;

            Rect rect = menu.worldBound;
            GenericMenu genericMenu = new GenericMenu();

            // Add scenes from the collection.
            for (int i = 0; i < SCENES_COLLECTION.Count; i++)
            {
                UnityScene unityScene = SCENES_COLLECTION[i];
                if (IsHiddenFromScenePicker(unityScene.ScenePath))
                    continue;
                genericMenu.AddItem(new GUIContent(unityScene.name), unityScene.IsLoaded(), HandleItemClicked, unityScene);
            }

            bool addedSeparator = false;
            // Add any open scenes not in the collection.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene openScene = SceneManager.GetSceneAt(i);
                if (SCENES_COLLECTION.Any(scene => scene.Scene == openScene))
                    continue;
                if (IsHiddenFromScenePicker(openScene.path))
                    continue;
                if (!addedSeparator)
                {
                    genericMenu.AddSeparator("");
                    addedSeparator = true;
                }
                string openSceneName = string.IsNullOrEmpty(openScene.name) ? $"Untitled Scene {i}" : openScene.name;
                genericMenu.AddItem(new GUIContent(openSceneName), true, HandleItemClicked, openScene);
            }

            genericMenu.DropDown(rect);
        }

        private static void HandleItemClicked(object parameter)
        {
            if (parameter is UnityScene unityScene)
            {
                if (unityScene.IsLoaded())
                {
                    if (CanUnload())
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
                        bool proceed = EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new Scene[] { scene });
                        if (proceed)
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

        public static bool CanUnload()
        {
            return SceneManager.sceneCount > 1;
        }

        private static void ApplyToolbarStyle(VisualElement element)
        {
            element.AddToClassList("unity-toolbar-button");
            element.AddToClassList("unity-editor-toolbar-element");
            element.RemoveFromClassList("unity-button");
            element.style.paddingRight = 8;
            element.style.paddingLeft = 8;
            element.style.justifyContent = Justify.Center;
            element.style.display = DisplayStyle.Flex;
            element.style.borderTopLeftRadius = 2;
            element.style.borderTopRightRadius = 2;
            element.style.borderBottomLeftRadius = 2;
            element.style.borderBottomRightRadius = 2;
            element.style.height = 19;
            element.style.marginRight = 1;
            element.style.marginLeft = 1;
        }
    }
}