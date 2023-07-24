using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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
            bool isMacOS =
                System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform
                    .OSX);

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
                string[] possiblePaths =
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
                bool isMacOS =
                    System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices
                        .OSPlatform.OSX);

                ProcessStartInfo processStartInfo;

                // For macOS, run the command through the shell.
                if (isMacOS)
                {
                    processStartInfo =
                        new ProcessStartInfo("/bin/sh", "-c \"" + GitExecutable + " " + arguments + "\"");
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

        public static string GetCurrentBranch()
        {
            // Command: git rev-parse --abbrev-ref HEAD
            RunGitCommand("rev-parse --abbrev-ref HEAD", out string result);

            if (string.IsNullOrEmpty(result))
                return string.Empty;

            result = result.Trim();

            // If the result is "HEAD", then it means the repository is in a detached state.
            // We need to find out the branch it's tracking.
            if (result == "HEAD")
            {
                RunGitCommand("log -n 1 --pretty=%d HEAD", out result);
                if (string.IsNullOrEmpty(result))
                    return string.Empty;
            }

            // Use regex to extract the branch name.
            // This regex will match both direct branch names and names prefixed with 'origin/'
            var match = Regex.Match(result, @"(?:origin\/)?([a-zA-Z0-9_-]+)");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }


        public static GitCommitInfo GetLastCommit(Object asset)
        {
            return GetLastCommit(AssetDatabase.GetAssetPath(asset));
        }

        public static GitCommitInfo GetLastCommit(string assetPath)
        {
            string gitRoot = GetGitRootDirectory();
            string relativeFilePath = MakeRelativePath(gitRoot, assetPath);

            return GetCommit("-n 1", relativeFilePath).FirstOrDefault();
        }

        public static GitCommitInfo GetFirstCommit(Object asset)
        {
            return GetFirstCommit(AssetDatabase.GetAssetPath(asset));
        }

        public static GitCommitInfo GetFirstCommit(string assetPath)
        {
            string gitRoot = GetGitRootDirectory();
            string relativeFilePath = MakeRelativePath(gitRoot, assetPath);

            return GetCommit("--diff-filter=A", relativeFilePath).FirstOrDefault();
        }

        private static string GetGitRootDirectory()
        {
            // This command gets the root directory of the git repository
            RunGitCommand("rev-parse --show-toplevel", out string gitRoot);
            return gitRoot?.TrimEnd('\n');
        }

        private static string MakeRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath))
                throw new ArgumentNullException(nameof(fromPath));
            if (string.IsNullOrEmpty(toPath))
                throw new ArgumentNullException(nameof(toPath));

            fromPath = Path.GetFullPath(fromPath); // Normalize paths
            toPath = Path.GetFullPath(toPath);

            // Convert to URI format
            Uri fromUri = new Uri(fromPath + Path.DirectorySeparatorChar);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            // Convert URI directory separators to OS-specific separators
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        private static List<GitCommitInfo> GetCommit(string query, string fileName)
        {
            string commandFormat = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                ? "log {0} --follow --format=\"%an|%aD|%s\" -- \"{1}\""
                : "log {0} --follow --format='%an|%aD|%s' -- '{1}'";

            string gitCommand = string.Format(commandFormat, query, fileName);

            RunGitCommand(gitCommand, out string commandOutput);

            if (commandOutput == null)
                return null;

            string[] lines = commandOutput.Split('\n');

            List<GitCommitInfo> result = new List<GitCommitInfo>();

            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                if (parts.Length < 3)
                    continue;

                string name = parts[0];
                DateTime dateTime;

                // This is just an example date format. You should adjust this if Git's date format is different.
                DateTime.TryParseExact(parts[1], "ddd MMM dd HH:mm:ss yyyy zzz",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);

                string title = parts[2];

                result.Add(new GitCommitInfo(name, dateTime, title));
            }

            return result;
        }
    }
}