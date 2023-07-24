using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Git
{
    public static class GitUtilitiestSettingsRegister
    {
        private static SceneAsset bootstrapScene;
        private static string displayResult;

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
                        EditorGUILayout.HelpBox("Git cannot be found. Please select the git executable below",
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

                    if (GUILayout.Button("Auto detect git executable", GUILayout.Width(200)))
                    {
                        GitUtility.FindGitExecutable();
                    }


                    EditorGUILayout.TextArea(displayResult);
                    
                    if (GUILayout.Button("Get Current Branch", GUILayout.Width(200)))
                    {
                        displayResult = GitUtility.GetCurrentBranch();
                    }
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Editor", "Git", "Settings" })
            };

            return provider;
        }
    }
}