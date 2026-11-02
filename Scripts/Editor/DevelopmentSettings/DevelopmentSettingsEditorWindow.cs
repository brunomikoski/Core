using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    internal static class DevelopmentSettingsEditorGUI
    {
        public static void DrawSettings()
        {
            DrawSettings(typeof(DevelopmentSettings));

            foreach (Type groupType in TypeCache.GetTypesWithAttribute<DevelopmentSettingsGroupAttribute>())
            {
                string label = groupType.GetCustomAttribute<DevelopmentSettingsGroupAttribute>().DisplayName;
                if (string.IsNullOrEmpty(label))
                    label = StringUtility.GetHumanReadableText(groupType.Name);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                DrawSettings(groupType);
            }
        }

        private static void DrawSettings(Type settingsType)
        {
            FieldInfo[] fieldInfos = settingsType.GetFields(BindingFlags.Public | BindingFlags.Static);

            for (int i = 0; i < fieldInfos.Length; ++i)
            {
                FieldInfo info = fieldInfos[i];
                object setting = info.GetValue(null);

                if (setting is DevelopmentSetting booleanSetting)
                {
                    DrawBooleanSetting(info, booleanSetting);
                    continue;
                }

                DrawGenericSetting(info, setting);
            }
        }

        private static void DrawBooleanSetting(FieldInfo info, DevelopmentSetting setting)
        {
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.ToggleLeft(StringUtility.GetHumanReadableText(info.Name), setting);

            if (EditorGUI.EndChangeCheck())
            {
                setting.Value = newValue;
            }
        }

        private static void DrawGenericSetting(FieldInfo info, object setting)
        {
            Type settingType = info.FieldType;
            if (!settingType.IsGenericType ||
                settingType.GetGenericTypeDefinition() != typeof(DevelopmentSettingGeneric<>))
            {
                return;
            }

            Type valueType = settingType.GetGenericArguments()[0];
            PropertyInfo valueProperty = settingType.GetProperty(nameof(DevelopmentSettingGeneric<int>.Value));
            string label = StringUtility.GetHumanReadableText(info.Name);

            if (valueType == typeof(bool))
            {
                bool currentBool = (bool)valueProperty.GetValue(setting);

                EditorGUI.BeginChangeCheck();
                bool newBool = EditorGUILayout.ToggleLeft(label, currentBool);

                if (EditorGUI.EndChangeCheck())
                {
                    valueProperty.SetValue(setting, newBool);
                }

                return;
            }

            if (valueType == typeof(string))
            {
                string currentString = (string)valueProperty.GetValue(setting);

                EditorGUI.BeginChangeCheck();
                string newString = EditorGUILayout.DelayedTextField(label, currentString);

                if (EditorGUI.EndChangeCheck())
                {
                    valueProperty.SetValue(setting, newString);
                }

                return;
            }

            if (valueType.IsEnum)
            {
                Enum currentValue = (Enum)valueProperty.GetValue(setting);

                EditorGUI.BeginChangeCheck();
                Enum newValue;
                if (valueType.IsDefined(typeof(FlagsAttribute), false))
                    newValue = EditorGUILayout.EnumFlagsField(label, currentValue);
                else
                    newValue = EditorGUILayout.EnumPopup(label, currentValue);

                if (EditorGUI.EndChangeCheck())
                {
                    valueProperty.SetValue(setting, newValue);
                }
            }
        }
    }

    public class DevelopmentSettingsEditorWindow : EditorWindow
    {
        [SerializeField] private Vector2 _scrollPosition;

        [MenuItem("Window/Development Settings")]
        [MenuItem("Tools/Git/Development Settings", priority = 60000)]
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
            DevelopmentSettingsEditorGUI.DrawSettings();

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }
    }
}
