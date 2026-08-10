using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public class DevelopmentSettingsPopupWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private static List<MethodInfo> _exposedMethodsCache;
        private static List<string> _exposedMethodLabelsCache;

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
            _exposedMethodsCache = new List<MethodInfo>();
            _exposedMethodLabelsCache = new List<string>();

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

                _exposedMethodsCache.Add(method);
                _exposedMethodLabelsCache.Add(label);
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Development Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Configure settings for local testing", EditorStyles.miniLabel);
            EditorGUILayout.Space(8);
            
            DevelopmentSettingsEditorGUI.DrawSettings();

            if (_exposedMethodsCache == null)
                RefreshExposedMethods();

            if (_exposedMethodsCache != null && _exposedMethodsCache.Count > 0)
            {
                EditorGUILayout.Space(12);
                EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                for (int i = 0; i < _exposedMethodsCache.Count; i++)
                {
                    if (GUILayout.Button(_exposedMethodLabelsCache[i], GUILayout.Height(22)))
                    {
                        try
                        {
                            _exposedMethodsCache[i].Invoke(null, null);
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
}
