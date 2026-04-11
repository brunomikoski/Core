using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
#if !UNITY_6000_3_OR_NEWER
using BrunoMikoski.Core.Utils;
#endif

namespace BrunoMikoski.Core
{
    public static class DevelopmentSettingsToolbar
    {
#if UNITY_6000_3_OR_NEWER

        private static class MainToolbarRegistration
        {
            private const string ToolbarElementPath = "BrunoMikoski/DevelopmentSettings";

            [MainToolbarElement(ToolbarElementPath, defaultDockPosition = MainToolbarDockPosition.Right)]
            public static MainToolbarElement CreateDevelopmentSettingsButton()
            {
                Texture2D icon = ResolveToolbarIcon();
                var content = new MainToolbarContent("", icon, "Development settings");
                return new MainToolbarButton(content, DevelopmentSettingsPopupWindow.Show);
            }
        }

#else

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += SetupToolbarButton;
        }

        private static void SetupToolbarButton()
        {
            if (!UnityMainToolbarUtility.IsToolbarAvailable())
                return;

            UnityMainToolbarUtility.AddButton(
                UnityMainToolbarUtility.TargetContainer.Right,
                UnityMainToolbarUtility.Side.Right,
                "DevelopmentSettings",
                ResolveToolbarIcon(),
                OnButtonClicked
            );
        }

        private static void OnButtonClicked()
        {
            DevelopmentSettingsPopupWindow.Show();
        }

#endif

        private static Texture2D ResolveToolbarIcon()
        {
            string[] iconNames = { "Settings", "d_Settings", "SettingsIcon", "d_SettingsIcon" };

            foreach (string iconName in iconNames)
            {
                GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
                if (iconContent != null && iconContent.image != null)
                {
                    Texture2D icon = iconContent.image as Texture2D;
                    if (icon != null)
                        return icon;
                }
            }

            return EditorGUIUtility.IconContent("d_EditorSettings Icon").image as Texture2D;
        }
    }
}
