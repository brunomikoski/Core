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
        private static UnityScenesCollection SCENES_COLLECTION;
        private static readonly GUIContent SCENE_ICON;
        private static GUIStyle GUI_STYLE;

        static ScenePickerToolbar()
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
            for (int i = 0; i < SCENES_COLLECTION.Count; i++)
            {
                UnityScene unityScene = SCENES_COLLECTION[i];
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
