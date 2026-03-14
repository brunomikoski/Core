using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public static class EditorBootstrapSettingsProvider
    {
        private static SceneAsset[] requiredScenes = Array.Empty<SceneAsset>();
        private static ReorderableList reorderableList;

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider("Project/Editor Bootstrap", SettingsScope.Project)
            {
                label = "Editor Bootstrap",
                guiHandler = (searchContext) =>
                {
                    EditorBootstrapSettings settings = EditorBootstrapSettings.Instance;

                    if ((requiredScenes.Length == 0 || requiredScenes.Length != settings.BootStrapScenesGuid.Count) && settings.HasBootstrapSetup())
                    {
                        requiredScenes = new SceneAsset[settings.BootStrapScenesGuid.Count];
                        for (int i = 0; i < settings.BootStrapScenesGuid.Count; i++)
                        {
                            requiredScenes[i] = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(settings.BootStrapScenesGuid[i]));
                        }
                    }

                    if (reorderableList == null || reorderableList.count != requiredScenes.Length)
                    {
                        reorderableList = new ReorderableList(requiredScenes, typeof(SceneAsset), true, true, true, true)
                        {
                            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Required Scenes"),
                            drawElementCallback = (rect, index, isActive, isFocused) =>
                            {
                                if (index < requiredScenes.Length)
                                {
                                    requiredScenes[index] = (SceneAsset)EditorGUI.ObjectField(
                                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                                        requiredScenes[index],
                                        typeof(SceneAsset),
                                        false
                                    );
                                }
                            }
                        };
                        reorderableList.onAddCallback = list =>
                        {
                            List<SceneAsset> sceneList = new(requiredScenes) { null };
                            requiredScenes = sceneList.ToArray();
                            reorderableList.list = requiredScenes;
                        };
                        reorderableList.onRemoveCallback = list =>
                        {
                            List<SceneAsset> sceneList = new(requiredScenes);
                            if (list.index >= 0 && list.index < sceneList.Count)
                            {
                                sceneList.RemoveAt(list.index);
                                requiredScenes = sceneList.ToArray();
                                reorderableList.list = requiredScenes;
                            }
                        };
                    }

                    EditorGUI.BeginChangeCheck();

                    reorderableList.DoLayoutList();

                    settings.LoadOpenScenesAfterBootstrap = EditorGUILayout.Toggle("Load Open Scenes After Bootstrap", settings.LoadOpenScenesAfterBootstrap);

                    if (EditorGUI.EndChangeCheck())
                    {
                        // Save settings when changes occur
                        settings.BootStrapScenesGuid.Clear();
                        foreach (SceneAsset scene in requiredScenes)
                        {
                            if (scene != null)
                            {
                                string scenePath = AssetDatabase.GetAssetPath(scene);
                                string sceneGuid = AssetDatabase.AssetPathToGUID(scenePath);
                                settings.BootStrapScenesGuid.Add(sceneGuid);
                            }
                        }

                        settings.Save();
                    }
                },

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


        public List<string> BootStrapScenesGuid = new List<string>();
        public bool LoadOpenScenesAfterBootstrap = true;

        public bool HasBootstrapSetup()
        {
            return BootStrapScenesGuid.Count > 0;
        }

        public void Save()
        {
            string json = EditorJsonUtility.ToJson(this, prettyPrint: true);
            File.WriteAllText(STORAGE_PATH, json);
        }
    }
}
