using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public class DevelopmentSettingsEditorWindow : EditorWindow
    {
        [SerializeField] private Vector2 _scrollPosition;

        [MenuItem("Window/Development Settings")]
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
}
