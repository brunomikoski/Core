using BrunoMikoski.Core.SelectionHistory;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BrunoMikoski.Playground.ScenesReferences
{
    [InitializeOnLoad]
    public class ScenePickerToolbar
    {
        static ScenePickerToolbar()
        {
            EditorApplication.delayCall += Initialize;
        }
        
        private static void Initialize()
        {
            GUI_STYLE = new GUIStyle(EditorStyles.toolbarPopup)
            {
                fixedHeight = 19,
                fixedWidth = 100,
            };
            
            sceneIcon = EditorGUIUtility.IconContent("d_SceneAsset Icon");
            sceneIcon.text = "Scenes";
            
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

        private static UnityScenesCollection scenesCollection;
        private static GUIContent sceneIcon;
        private static GUIStyle GUI_STYLE;

        private static void DropDownIMGUI()
        {
            if (!CollectionsRegistry.Instance.TryGetCollectionOfType(out scenesCollection))
                return;

            
            if (!EditorGUILayout.DropdownButton(sceneIcon, FocusType.Passive, GUI_STYLE))
            {
                return;
            }
 
            void HandleItemClicked(object parameter)
            {
                UnityScene unityScene = (UnityScene) parameter;
                if (unityScene.IsLoaded())
                {
                    if(CanUnload())
                       unityScene.Unload();
                }
                else
                    unityScene.Load();
            }
 
            GenericMenu menu = new();
            for (int i = 0; i < scenesCollection.Count; i++)
            {
                UnityScene unityScene = scenesCollection[i];
                menu.AddItem(new GUIContent(unityScene.name), unityScene.IsLoaded(), HandleItemClicked, unityScene);
            }

            menu.DropDown(EditorGUILayout.GetControlRect());
        }

        private static bool CanUnload()
        {
            return SceneManager.sceneCount > 1;
        }
    }
}