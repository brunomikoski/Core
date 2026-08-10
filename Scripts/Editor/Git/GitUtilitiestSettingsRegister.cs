using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Git
{
    public static class GitUtilitiestSettingsRegister
    {
        private static SceneAsset bootstrapScene;
        private static string displayResult;
        private static Object selectedObjectField;

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            SettingsProvider provider = new("Git Utility", SettingsScope.User)
            {
                label = "Git Utility",
                guiHandler = (_) =>
                {
                    if (GitUtility.IsGitInstalled())
                    {
                        EditorGUILayout.HelpBox("Git is installed", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Git cannot be found. 'git' from PATH is used by default — set a specific executable below, or run Tools/Setup Git Tools to locate a GUI client's bundled git.",
                            MessageType.Error);
                    }

                    EditorGUILayout.BeginHorizontal();

                    GitUtility.GitExecutable = EditorGUILayout.DelayedTextField("Git executable path", GitUtility.GitExecutable);

                    if (GUILayout.Button("Browse", GUILayout.Width(100)))
                    {
                        string result =
                            EditorUtility.OpenFilePanel("Git executable", Path.GetDirectoryName(GitUtility.GitExecutable), "exe");
                        
                        if (!string.IsNullOrEmpty(result))
                            GitUtility.GitExecutable = result;
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Get Current Branch", GUILayout.Width(200)))
                    {
                        displayResult = GitUtility.GetCurrentBranch();
                    }

                    selectedObjectField = EditorGUILayout.ObjectField("File", selectedObjectField, typeof(TextAsset), false);
                    
                    if (GUILayout.Button("Check file first commit", GUILayout.Width(200)))
                    {
                        displayResult = GitUtility.GetFirstCommit(AssetDatabase.GetAssetPath(selectedObjectField)).ToString();
                    }
                    
                    if (GUILayout.Button("Check file last commit", GUILayout.Width(200)))
                    {
                        displayResult = GitUtility.GetLastCommit(AssetDatabase.GetAssetPath(selectedObjectField)).ToString();
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.TextArea(displayResult, new[]{GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)});
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Editor", "Git", "Settings" })
            };

            return provider;
        }
    }
}