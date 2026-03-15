using System;
using System.Collections.Generic;
using System.Reflection;
using BrunoMikoski.Core.Utils;
using UnityEditor;
using UnityEngine;
using BrunoMikoski.Core.UnityToolbar;

namespace BrunoMikoski.Core.Development
{
    public class DevelopmentSettingsPopupWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private static List<MethodInfo> exposedMethodsCache;
        private static List<string> exposedMethodLabelsCache;

        public new static void Show()
        {
            DevelopmentSettingsPopupWindow window = GetWindow<DevelopmentSettingsPopupWindow>(true, "Development Settings", true);
            
            window.minSize = new Vector2(320, 260);
            window.maxSize = new Vector2(520, 800);
            
            ((EditorWindow)window).Show();
            window.Focus();
        }

        private static void RefreshExposedMethods()
        {
            exposedMethodsCache = new List<MethodInfo>();
            exposedMethodLabelsCache = new List<string>();

            TypeCache.MethodCollection methods = TypeCache.GetMethodsWithAttribute<ExposeOnDeveloperSettingsAttribute>();
            foreach (MethodInfo method in methods)
            {
                if (!method.IsStatic || method.GetParameters().Length > 0)
                {
                    Debug.LogWarning($"[ExposeOnDeveloperSettings] Skipping {method.DeclaringType?.Name}.{method.Name}: only static parameterless methods are supported.");
                    continue;
                }

                ExposeOnDeveloperSettingsAttribute attr = method.GetCustomAttribute<ExposeOnDeveloperSettingsAttribute>();
                string label = attr.DisplayName;
                if (string.IsNullOrEmpty(label))
                {
                    string typeName = method.DeclaringType?.Name ?? method.Name;
                    if (typeName.EndsWith("Window", StringComparison.OrdinalIgnoreCase))
                        typeName = typeName.Substring(0, typeName.Length - 6);
                    label = StringUtility.GetHumanReadableText(typeName);
                }

                exposedMethodsCache.Add(method);
                exposedMethodLabelsCache.Add(label);
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Development Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Configure settings for local testing", EditorStyles.miniLabel);
            EditorGUILayout.Space(8);
            
            FieldInfo[] fieldInfos = typeof(DevelopmentSettings).GetFields();

            for (int i = 0; i < fieldInfos.Length; ++i)
            {
                FieldInfo info = fieldInfos[i];
                DevelopmentSetting setting = (DevelopmentSetting)info.GetValue(null);

                EditorGUI.BeginChangeCheck();
                bool newValue = EditorGUILayout.ToggleLeft(
                    StringUtility.GetHumanReadableText(info.Name),
                    setting
                );

                if (EditorGUI.EndChangeCheck())
                {
                    setting.Value = newValue;
                }
            }

            if (exposedMethodsCache == null)
                RefreshExposedMethods();

            if (exposedMethodsCache != null && exposedMethodsCache.Count > 0)
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                for (int i = 0; i < exposedMethodsCache.Count; i++)
                {
                    if (GUILayout.Button(exposedMethodLabelsCache[i], GUILayout.Height(22)))
                    {
                        try
                        {
                            exposedMethodsCache[i].Invoke(null, null);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Close", GUILayout.Height(24)))
            {
                Close();
            }
        }
    }
    
    public class DevelopmentSettingsEditorWindow : EditorWindow
    {
        [SerializeField] private Vector2 _scrollPosition;

        [MenuItem("Window/Development Settings")]
        [MenuItem("Worker/Development Settings", priority = 60000)]
        private static void Init()
        {
            EditorWindow window = GetWindow(typeof(DevelopmentSettingsEditorWindow));
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Dev Settings");
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Development settings", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            FieldInfo[] fieldInfos = typeof(DevelopmentSettings).GetFields();

            for (int i = 0; i < fieldInfos.Length; ++i)
            {
                FieldInfo info = fieldInfos[i];

                DevelopmentSetting setting = (DevelopmentSetting)info.GetValue(null);

                EditorGUI.BeginChangeCheck();
                bool newValue
                    = EditorGUILayout.ToggleLeft(StringUtility.GetHumanReadableText(info.Name),
                        setting);

                if (EditorGUI.EndChangeCheck())
                {
                    setting.Value = newValue;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }
    }
    
    public static class DevelopmentSettingsToolbar
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += SetupToolbarButton;
        }

        private static void SetupToolbarButton()
        {
            if (!UnityMainToolbarUtility.IsToolbarAvailable())
                return;

            Texture2D icon = null;
            string[] iconNames = { "Settings", "d_Settings", "SettingsIcon", "d_SettingsIcon" };
            
            foreach (string iconName in iconNames)
            {
                GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
                if (iconContent != null && iconContent.image != null)
                {
                    icon = iconContent.image as Texture2D;
                    if (icon != null)
                        break;
                }
            }

            if (icon == null)
            {
                icon = EditorGUIUtility.IconContent("d_EditorSettings Icon").image as Texture2D;
            }

            UnityMainToolbarUtility.AddButton(
                UnityMainToolbarUtility.TargetContainer.Right,
                UnityMainToolbarUtility.Side.Right,
                "DevelopmentSettings",
                icon,
                OnButtonClicked
            );
        }

        private static void OnButtonClicked()
        {
            DevelopmentSettingsPopupWindow.Show();
        }
    }
}
