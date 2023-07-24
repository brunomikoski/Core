using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Debug = UnityEngine.Debug;

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
                    if (!IsGitInstalled())
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
            bool isMacOS = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);
            
            if (isMacOS)
            {
                string gitPath = GetGitPathThroughShell();
                if (!string.IsNullOrEmpty(gitPath))
                {
                    GitExecutable = gitPath;
                    if (IsGitInstalled())
                        return;
                }
            }
            else
            {
                string[] possiblePaths = {
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

                    if (!File.Exists(possiblePath)) 
                        continue;
                
                    GitExecutable = possiblePath;
                    if (IsGitInstalled())
                        return;
                }
            }
        }
#endif

        public static int? RunGitCommand(string arguments, out string result)
        {
            try
            {
                bool isMacOS = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);

                ProcessStartInfo processStartInfo;

                // For macOS, run the command through the shell.
                if (isMacOS)
                {
                    processStartInfo = new ProcessStartInfo("/bin/sh", "-c \"" + GitExecutable + " " + arguments + "\"");
                }
                else
                {
                    // For Windows, keep the original approach.
                    processStartInfo = new ProcessStartInfo(GitExecutable, arguments);
                }

                processStartInfo.UseShellExecute = false;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.CreateNoWindow = true;

                Process process = Process.Start(processStartInfo);

                if (process == null)
                {
                    result = null;
                    return null;
                }

                if (!process.WaitForExit(20000)) // Wait for 20 seconds
                {
                    process.Kill();
                    lastError = "Timeout reached";
                    result = null;
                    return null;
                }

                // Read error first, if any.
                if (process.ExitCode != 0)
                {
                    lastError = process.StandardError.ReadToEnd();
                    Debug.LogError("Git Error: " + lastError);
                    result = null;
                    return null;
                }

                // If no errors, get the standard output.
                result = process.StandardOutput.ReadToEnd();
                return process.ExitCode;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error running git command: " + ex.Message);
                result = null;
                return null;
            }
        }

        private static string GetGitPathThroughShell()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("which", "git");
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                Process p = Process.Start(psi);
                string gitPath = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();

                if (!string.IsNullOrEmpty(gitPath) && File.Exists(gitPath))
                {
                    return gitPath;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsGitInstalled()
        {
            isInstalledCachedExecutable = "";
            if (isInstalledCachedExecutable == gitExecutable)
            {
                return isInstalledCached;
            }

            int? exitCode = RunGitCommand("--version");

            isInstalledCachedExecutable = GitExecutable;
            isInstalledCached = exitCode.HasValue && exitCode.Value == 0;

            return isInstalledCached;
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

        public static string GetCurrentBranch()
        {
            // Command: git rev-parse --abbrev-ref HEAD
            RunGitCommand("rev-parse --abbrev-ref HEAD", out string result);

            if (string.IsNullOrEmpty(result))
                return string.Empty;

            result = result.Trim();

            // If the result is not HEAD, then it's likely the branch name directly.
            if (result != "HEAD")
                return ExtractBranchName(result);

            // If the result is "HEAD", then it means the repository is in a detached state.
            // We need to find out the branch it's tracking.
            RunGitCommand("log -n 1 --pretty=%d HEAD", out result);

            if (string.IsNullOrEmpty(result))
                return string.Empty;

            // Use regex to extract the branch name from results like: (HEAD, origin/development)
            var match = Regex.Match(result, @"origin\/([a-zA-Z0-9_-]+)");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
        
        private static string ExtractBranchName(string branch)
        {
            // Here, we'll remove 'origin/' prefix and return the branch name.
            return branch.Replace("origin/", "").Trim();
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
    }
}
