using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Git
{
    public static class GitUtility
    {
        private const string GIT_EXECUTABLE_KEY = "GitUtility/Executable";

        private static string lastError;
        public static string LastError => lastError;

        private static bool isInstalledCached;
        private static string isInstalledCachedExecutable = "hs7822ekhk8sdhdsa"; // random string
        private static string gitExecutable;
        public static string GitExecutable
        {
            get
            {
                if (gitExecutable == null)
                {
#if UNITY_EDITOR
                    gitExecutable = EditorPrefs.GetString(GIT_EXECUTABLE_KEY, null);
                    if (!IsGitInstalled)
                        FindGitExecutable();
#endif
                }
                return gitExecutable;
            }
            set
            {
                if (gitExecutable == value)
                    return;

                SetGitExecutable(value);
            }
        }

        private static void SetGitExecutable(string value)
        {
            gitExecutable = value;
#if UNITY_EDITOR
            EditorPrefs.SetString(GIT_EXECUTABLE_KEY, gitExecutable);
#endif
        }

#if UNITY_EDITOR
        public static void FindGitExecutable()
        {
            string[] possiblePaths = new[]
            {
                "git",
                "%programfiles(x86)%\\SmartGit\\git\\mingw64\\bin\\git.exe",
                "%programfiles%\\SmartGit\\git\\mingw64\\bin\\git.exe",
                "%programfiles(x86)%\\SmartGit\\git\\mingw32\\bin\\git.exe",
                "%programfiles%\\SmartGit\\git\\mingw32\\bin\\git.exe",
                "%programfiles(x86)%\\Git\\mingw64\\bin\\git.exe",
                "%programfiles%\\Git\\mingw64\\bin\\git.exe",
                "%programfiles(x86)%\\Git\\mingw32\\bin\\git.exe",
                "%programfiles%\\Git\\mingw32\\bin\\git.exe",
                "%localappdata%\\Atlassian\\SourceTree\\git_local\\mingw32\\bin\\git.exe",
                "%localappdata%\\Atlassian\\SourceTree\\git_local\\mingw64\\bin\\git.exe",
                "git",
            };

            for (int i = 0; i < possiblePaths.Length; i++)
            {
                string possiblePath = Environment.ExpandEnvironmentVariables(possiblePaths[i]);
                gitExecutable = possiblePath;
                if (IsGitInstalled)
                {
                    SetGitExecutable(possiblePath);
                    return;
                }
            }
        }
#endif

        public static int? RunGitCommand(string arguments, out string result)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo("\"" + GitExecutable + "\"", arguments);
                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.CreateNoWindow = true;
                Process process = Process.Start(processStartInfo);

                result = null;

                if (process == null)
                    return null;

                try
                {
                    if (!process.WaitForExit(20000))
                    {
                        process.Kill();
                        lastError = "Timeout reached";
                        return null;
                    }
                }
                catch (ThreadAbortException)
                {
                    if (!process.HasExited)
                        process.Kill();
                    throw;
                }

                if (process.ExitCode != 0)
                {
                    lastError = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    return null;
                }

                result = process.StandardOutput.ReadToEnd();

                return process.ExitCode;
            }
            catch
            {
                result = null;
                return null;
            }
        }

        public static bool IsGitInstalled
        {
            get
            {
                if (isInstalledCachedExecutable == gitExecutable)
                {
                    return isInstalledCached;
                }

                int? exitCode = RunGitCommand("help");

                isInstalledCachedExecutable = GitExecutable;
                isInstalledCached = exitCode.HasValue && exitCode.Value == 0;

                return isInstalledCached;
            }
        }

        public static int? RunGitCommand(string arguments)
        {
            return RunGitCommand(arguments, out string _);
        }

        public static string CurrentBranch => GetCurrentBranch();

        public static string CurrentCommitID
        {
            get
            {
                string result;
                RunGitCommand("rev-parse HEAD", out result);
                return result?.Trim();
            }
        }

        private static List<GitCommitInfo> GetCommit(string query, string fileName)
        {
            RunGitCommand("log " + query + " --follow --format=\"%an|%aD|%s\" -- \"" + fileName + "\"", out string commandOutput);

            if (commandOutput == null)
                return null;

            string[] lines = commandOutput.Split('\n');

            List<GitCommitInfo> result = new List<GitCommitInfo>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] parts = line.Split('|');

                if (parts.Length < 3)
                    continue;

                string name = parts[0];
                DateTime dateTime;

                DateTime.TryParse(parts[1], out dateTime);

                string title = parts[2];

                result.Add(new GitCommitInfo(name, dateTime, title));
            }
            return result;
        }

        private static string GetCurrentBranch()
        {
            // Command: git rev-parse --abbrev-ref HEAD
            string result;
            RunGitCommand("rev-parse --abbrev-ref HEAD", out result);
            if (result == null)
                return string.Empty;

            result = result.Trim();
            if (result != "HEAD")
                return result;

            RunGitCommand("log -n 1 --pretty=%d HEAD", out result);

            // Result will be in format: (HEAD -> feature/Control_type_mapping, origin/feature/Control_type_mapping)
            // or: (HEAD, origin/development)

            result = result.Trim();

            // Remove brackets
            result = result.Substring(1, result.Length - 2);

            string[] parts = result.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0)
                    continue;

                if (part == "HEAD")
                    continue;

                part = part.Replace("HEAD", "")
                    .Replace("->", "").Trim();

                part = part.Replace("origin/", "").Trim();

                if (part.Length == 0)
                    continue;

                return part;
            }
            return string.Empty;
        }

        public static GitCommitInfo GetFirstCommit(string fileName)
        {
            return GetCommit("--diff-filter=A", fileName).FirstOrDefault();
        }

        public static GitCommitInfo GetLastCommit(string fileName)
        {
            return GetCommit("-n 1", fileName).FirstOrDefault();
        }

        public static List<GitCommitInfo> GetLastCommits(string fileName, int count)
        {
            return GetCommit("-n " + count, fileName);
        }
        
        
        
        

#if UNITY_EDITOR
        [PreferenceItem("Git")]
        private static void OnSettingsGUI()
        {
            if (IsGitInstalled)
            {
                EditorGUILayout.HelpBox("Git is installed", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Git cannot be found. Please select the git executable below",
                    MessageType.Error);
            }

            EditorGUILayout.BeginHorizontal();

            GitExecutable = EditorGUILayout.DelayedTextField("Git executable path", GitExecutable);

            if (GUILayout.Button("Browse", GUILayout.Width(100)))
            {
                string result =
                    EditorUtility.OpenFilePanel("Git executable", Path.GetDirectoryName(GitExecutable), "exe");
                if (!string.IsNullOrEmpty(result))
                    GitExecutable = result;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Auto detect git executable", GUILayout.Width(200)))
            {
                FindGitExecutable();
            }
        }
#endif
    }
}
