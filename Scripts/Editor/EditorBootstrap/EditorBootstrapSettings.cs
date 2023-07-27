using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace BrunoMikoski.Core
{
    public static class EditorBootstrapProjectSettingsRegister
    {
        private static SceneAsset bootstrapScene;
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            SettingsProvider provider = new("Project/Editor Bootstrap", SettingsScope.Project)
            {
                label = "Editor Bootstrap",
                guiHandler = (searchContext) =>
                {
                    EditorBootstrapSettings settings = EditorBootstrapSettings.Instance;

                    if (bootstrapScene == null && !string.IsNullOrEmpty(settings.BootStrapSceneGuid))
                        bootstrapScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(settings.BootStrapSceneGuid));
                    
                    EditorGUI.BeginChangeCheck();

                    SceneAsset newScene = EditorGUILayout.ObjectField("Services Bootstrap Scene", bootstrapScene, typeof(SceneAsset), false) as SceneAsset;
                    settings.LoadOpenScenesAfterBootstrap = EditorGUILayout.Toggle("Load Open Scenes After Bootstrap", settings.LoadOpenScenesAfterBootstrap);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.SetBootstrapScene(newScene);
                        
                        // Save settings to file whenever changes are made.
                        settings.Save();
                        bootstrapScene = newScene;
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Editor", "Bootstrap", "Settings", "Bootstrap Scene" })
            };

            return provider;
        }
    }
    
    
    [Serializable]
    public class EditorBootstrapSettings
    {
        private const string STORAGE_PATH = "ProjectSettings/EditorBootstrapSettings.json";

        private static EditorBootstrapSettings instance;
        public static EditorBootstrapSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    if (File.Exists(STORAGE_PATH))
                    {
                        // Load settings from file.
                        string json = File.ReadAllText(STORAGE_PATH);
                        instance = JsonUtility.FromJson<EditorBootstrapSettings>(json);
                    }
                    else
                    {
                        // Create new settings instance if file doesn't exist.
                        instance = new EditorBootstrapSettings();
                    }
                }
                return instance;
            }
        }


        public string BootStrapSceneGuid;
        public bool LoadOpenScenesAfterBootstrap = true;

        public bool HasBootstrapSetup()
        {
            return !string.IsNullOrEmpty(BootStrapSceneGuid);
        }

        public void Save()
        {
            string json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(STORAGE_PATH, json);
        }

        public void SetBootstrapScene(SceneAsset newScene)
        {
            if (newScene != null)
            {
                BootStrapSceneGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newScene));
            }
            else
            {
                BootStrapSceneGuid = string.Empty;
            }
        }
    }
}
