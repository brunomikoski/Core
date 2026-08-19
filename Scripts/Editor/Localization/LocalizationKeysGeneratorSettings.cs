#if UNITY_LOCALIZATION_AVAILABLE
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Utils.Localization
{
    public static class LocalizationKeysGeneratorSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider("Project/Localization Keys", SettingsScope.Project)
            {
                label = "Localization Keys",
                guiHandler = searchContext =>
                {
                    LocalizationKeysGeneratorSettings settings = LocalizationKeysGeneratorSettings.Instance;

                    EditorGUI.BeginChangeCheck();

                    string folder = EditorGUILayout.TextField(
                        new GUIContent("Generated Script Folder", "Project-relative folder where LocalizationKeys.g.cs is written, e.g. Assets/Generated/Code/Localization"),
                        settings.GeneratedScriptFolder);
                    string generatedNamespace = EditorGUILayout.TextField(
                        new GUIContent("Generated Script Namespace", "Namespace of the generated LocalizationKeys class"),
                        settings.GeneratedScriptNamespace);

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.GeneratedScriptFolder = folder;
                        settings.GeneratedScriptNamespace = generatedNamespace;
                        settings.Save();
                    }

                    if (!settings.IsConfigured())
                    {
                        EditorGUILayout.HelpBox(
                            "Set both values to enable Tools/Localization/Generate Localization Keys. Once configured, the keys regenerate automatically whenever a string table changes.",
                            MessageType.Info);
                    }
                },
                keywords = new HashSet<string>(new[] { "Localization", "Keys", "Generator", "Strings" })
            };

            return provider;
        }
    }

    [Serializable]
    public class LocalizationKeysGeneratorSettings
    {
        private const string STORAGE_PATH = "ProjectSettings/LocalizationKeysGeneratorSettings.json";

        private static LocalizationKeysGeneratorSettings instance;

        public static LocalizationKeysGeneratorSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    if (File.Exists(STORAGE_PATH))
                    {
                        string json = File.ReadAllText(STORAGE_PATH);
                        instance = JsonUtility.FromJson<LocalizationKeysGeneratorSettings>(json);
                    }
                    else
                    {
                        instance = new LocalizationKeysGeneratorSettings();
                    }
                }

                return instance;
            }
        }

        public string GeneratedScriptFolder = string.Empty;
        public string GeneratedScriptNamespace = string.Empty;

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(GeneratedScriptFolder) && !string.IsNullOrEmpty(GeneratedScriptNamespace);
        }

        public void Save()
        {
            string json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(STORAGE_PATH, json);
        }
    }
}
#endif
