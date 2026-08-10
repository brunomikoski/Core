using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.DevelopmentTools.Git
{
    public static class GitUtility
    {
        private const string GIT_EXECUTABLE_KEY = "GitUtility/Executable";
        private const string DefaultGitExecutable = "git";
        private const string CurrentGitCommitKey = "CURRENT_GIT_COMMIT";

        private static string LAST_ERROR;
        public static string LastError => LAST_ERROR;

        private static bool IS_INSTALLED_CACHED;
        private static string IS_INSTALLED_CACHED_EXECUTABLE = "hs7822ekhk8sdhdsa"; // random string
        private static string GIT_EXECUTABLE;
        private static string GIT_ROOT_DIRECTORY;

        public static string GitExecutable
        {
            get
            {
                if (string.IsNullOrEmpty(GIT_EXECUTABLE))
                {
#if UNITY_EDITOR
                    GIT_EXECUTABLE = EditorPrefs.GetString(GIT_EXECUTABLE_KEY, DefaultGitExecutable);
#endif
                    if (string.IsNullOrEmpty(GIT_EXECUTABLE))
                        GIT_EXECUTABLE = DefaultGitExecutable;
                }

                return GIT_EXECUTABLE;
            }
            set
            {
                if (GIT_EXECUTABLE == value)
                    return;

                SetGitExecutable(value);
            }
        }
        
        public static string CurrentGitCommit
        {
            get
            {
#if UNITY_EDITOR
                if (TryGetRepositoryHeadSha(out string sha))
                    return sha;

                return EditorPrefs.GetString(CurrentGitCommitKey, string.Empty);
#else
                return string.Empty;
#endif
            }
            set
            {
#if UNITY_EDITOR
                EditorPrefs.SetString(CurrentGitCommitKey, value);
#endif
            }
        }

        public static string StoredGitCommitBridge
        {
            get
            {
#if UNITY_EDITOR
                return EditorPrefs.GetString(CurrentGitCommitKey, string.Empty);
#else
                return string.Empty;
#endif
            }
        }

#if UNITY_EDITOR
        private const int ShortShaLength = 7;

        private static string cachedProjectGitDirectory;
        private static string cachedRefsRoot;
        private static string cachedHeadSha;
        private static string cachedHeadWatchedRefName;
        private static DateTime cachedHeadFileTime;
        private static DateTime cachedLooseRefFileTime;
        private static DateTime cachedPackedRefsFileTime;

        public static bool TryGetRepositoryHeadSha(out string shortSha)
        {
            shortSha = null;

            string gitDirectory = cachedProjectGitDirectory;
            if (string.IsNullOrEmpty(gitDirectory) || !File.Exists(Path.Combine(gitDirectory, "HEAD")))
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot) || !TryResolveGitDirectory(projectRoot, out gitDirectory))
                {
                    cachedProjectGitDirectory = null;
                    cachedRefsRoot = null;
                    cachedHeadSha = null;
                    return false;
                }

                cachedProjectGitDirectory = gitDirectory;
                cachedRefsRoot = ResolveRefsRoot(gitDirectory);
                cachedHeadSha = null;
            }

            string refsRoot = cachedRefsRoot ?? gitDirectory;

            DateTime headFileTime = File.GetLastWriteTimeUtc(Path.Combine(gitDirectory, "HEAD"));
            DateTime packedRefsFileTime = File.GetLastWriteTimeUtc(Path.Combine(refsRoot, "packed-refs"));

            if (cachedHeadSha != null && headFileTime == cachedHeadFileTime && packedRefsFileTime == cachedPackedRefsFileTime)
            {
                if (cachedHeadWatchedRefName == null)
                {
                    shortSha = cachedHeadSha;
                    return true;
                }

                DateTime looseRefFileTime = File.GetLastWriteTimeUtc(LooseRefPath(refsRoot, cachedHeadWatchedRefName));
                if (looseRefFileTime == cachedLooseRefFileTime)
                {
                    shortSha = cachedHeadSha;
                    return true;
                }
            }

            if (!TryReadHeadSha(gitDirectory, out string resolvedSha, out string watchedRefName))
            {
                cachedHeadSha = null;
                return false;
            }

            cachedHeadSha = resolvedSha;
            cachedHeadWatchedRefName = watchedRefName;
            cachedHeadFileTime = headFileTime;
            cachedPackedRefsFileTime = packedRefsFileTime;
            cachedLooseRefFileTime = watchedRefName == null
                ? default
                : File.GetLastWriteTimeUtc(LooseRefPath(refsRoot, watchedRefName));

            shortSha = resolvedSha;
            return true;
        }

        public static bool TryResolveGitDirectory(string projectRoot, out string gitDirectory)
        {
            gitDirectory = null;

            if (string.IsNullOrEmpty(projectRoot))
                return false;

            try
            {
                string gitPath = Path.Combine(projectRoot, ".git");
                if (Directory.Exists(gitPath))
                {
                    gitDirectory = gitPath;
                    return true;
                }

                if (!File.Exists(gitPath))
                    return false;

                string firstLine;
                using (StreamReader reader = new StreamReader(gitPath))
                    firstLine = reader.ReadLine();

                const string gitDirPrefix = "gitdir:";
                if (firstLine == null || !firstLine.StartsWith(gitDirPrefix, StringComparison.Ordinal))
                    return false;

                string target = firstLine.Substring(gitDirPrefix.Length).Trim();
                if (string.IsNullOrEmpty(target))
                    return false;

                if (!Path.IsPathRooted(target))
                    target = Path.GetFullPath(Path.Combine(projectRoot, target));

                if (!Directory.Exists(target))
                    return false;

                gitDirectory = target;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryReadHeadSha(string gitDirectory, out string shortSha, out string watchedRefName)
        {
            shortSha = null;
            watchedRefName = null;

            try
            {
                string headPath = Path.Combine(gitDirectory, "HEAD");
                if (!File.Exists(headPath))
                    return false;

                string headContent = File.ReadAllText(headPath).Trim();

                const string refPrefix = "ref: ";
                if (!headContent.StartsWith(refPrefix, StringComparison.Ordinal))
                {
                    if (!IsHexSha(headContent))
                        return false;

                    shortSha = ToShortSha(headContent);
                    return true;
                }

                string refName = headContent.Substring(refPrefix.Length).Trim();
                if (string.IsNullOrEmpty(refName))
                    return false;

                watchedRefName = refName;

                string refsRoot = ResolveRefsRoot(gitDirectory);
                string looseRefPath = LooseRefPath(refsRoot, refName);
                if (File.Exists(looseRefPath))
                {
                    string looseContent = File.ReadAllText(looseRefPath).Trim();
                    if (IsHexSha(looseContent))
                    {
                        shortSha = ToShortSha(looseContent);
                        return true;
                    }
                }

                string packedRefsPath = Path.Combine(refsRoot, "packed-refs");
                if (!File.Exists(packedRefsPath))
                    return false;

                foreach (string line in File.ReadLines(packedRefsPath))
                {
                    if (string.IsNullOrEmpty(line) || line[0] == '#' || line[0] == '^')
                        continue;

                    int separatorIndex = line.IndexOf(' ');
                    if (separatorIndex <= 0)
                        continue;

                    if (!string.Equals(line.Substring(separatorIndex + 1).Trim(), refName, StringComparison.Ordinal))
                        continue;

                    string packedSha = line.Substring(0, separatorIndex);
                    if (!IsHexSha(packedSha))
                        return false;

                    shortSha = ToShortSha(packedSha);
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                shortSha = null;
                return false;
            }
        }

        private static string ResolveRefsRoot(string gitDirectory)
        {
            try
            {
                string commonDirPath = Path.Combine(gitDirectory, "commondir");
                if (!File.Exists(commonDirPath))
                    return gitDirectory;

                string firstLine;
                using (StreamReader reader = new StreamReader(commonDirPath))
                    firstLine = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(firstLine))
                    return gitDirectory;

                string target = firstLine.Trim();
                if (!Path.IsPathRooted(target))
                    target = Path.GetFullPath(Path.Combine(gitDirectory, target));

                return Directory.Exists(target) ? target : gitDirectory;
            }
            catch (Exception)
            {
                return gitDirectory;
            }
        }

        private static string LooseRefPath(string refsRoot, string refName)
        {
            return Path.Combine(refsRoot, refName.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool IsHexSha(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < ShortShaLength || value.Length > 64)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex)
                    return false;
            }

            return true;
        }

        private static string ToShortSha(string sha)
        {
            return sha.Length <= ShortShaLength ? sha : sha.Substring(0, ShortShaLength);
        }
#endif

        private static void SetGitExecutable(string value)
        {
            GIT_EXECUTABLE = value;
#if UNITY_EDITOR
            EditorPrefs.SetString(GIT_EXECUTABLE_KEY, GIT_EXECUTABLE);
#endif
        }

        public static int? RunGitCommand(string arguments, out string result)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(GitExecutable, arguments);

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
                    LAST_ERROR = "Timeout reached";
                    result = null;
                    return null;
                }

                // Read error first, if any.
                if (process.ExitCode != 0)
                {
                    LAST_ERROR = process.StandardError.ReadToEnd();
                    Debug.LogError("Git Error: " + LAST_ERROR);
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

        public static bool IsGitInstalled()
        {
            if (string.Equals(IS_INSTALLED_CACHED_EXECUTABLE, GitExecutable, StringComparison.Ordinal))
            {
                return IS_INSTALLED_CACHED;
            }

            int? exitCode = RunGitCommand("--version");

            IS_INSTALLED_CACHED_EXECUTABLE = GitExecutable;
            IS_INSTALLED_CACHED = exitCode == 0;

            return IS_INSTALLED_CACHED;
        }

        public static int? RunGitCommand(string arguments)
        {
            return RunGitCommand(arguments, out string _);
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

        // Branches (local and remote) whose history contains the given commit. The '+' prefix git
        // emits for worktree-checked-out branches is stripped along with the current-branch '*'.
        public static List<string> GetBranchesContaining(string sha)
        {
            List<string> branches = new List<string>();

            if (string.IsNullOrEmpty(sha))
                return branches;

            RunGitCommand($"branch --all --contains {sha}", out string result);
            if (string.IsNullOrEmpty(result))
                return branches;

            foreach (string line in result.Split('\n'))
            {
                string branch = line.Trim().TrimStart('*', '+').Trim();
                if (string.IsNullOrEmpty(branch) || branch.Contains("->"))
                    continue;

                branches.Add(branch);
            }

            return branches;
        }

        // True when the working tree has uncommitted changes (staged or unstaged) under the path.
        public static bool HasUncommittedChanges(string path)
        {
            string gitRoot = GetGitRootDirectory();
            if (string.IsNullOrEmpty(gitRoot))
                return false;

            string relativePath = MakeRelativePath(gitRoot, path);

            string command = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                ? $"status --porcelain -- \"{relativePath}\""
                : $"status --porcelain -- '{relativePath}'";

            RunGitCommand(command, out string result);
            return !string.IsNullOrWhiteSpace(result);
        }

#if UNITY_EDITOR
        public static GitCommitInfo GetLastCommit(Object asset)
        {
            return GetLastCommit(AssetDatabase.GetAssetPath(asset));
        }
#endif

        // follow tracks the file across renames; it only works for single files, so pass false
        // when assetPath is a folder.
        public static GitCommitInfo GetLastCommit(string assetPath, bool follow = true)
        {
            string gitRoot = GetGitRootDirectory();
            if (string.IsNullOrEmpty(gitRoot))
                return null;

            string relativeFilePath = MakeRelativePath(gitRoot, assetPath);

            return GetCommit("-n 1", relativeFilePath, follow)?.FirstOrDefault();
        }
        
#if UNITY_EDITOR
        public static GitCommitInfo GetFirstCommit(Object asset)
        {
            return GetFirstCommit(AssetDatabase.GetAssetPath(asset));
        }
#endif

        public static GitCommitInfo GetFirstCommit(string assetPath, bool follow = true)
        {
            string gitRoot = GetGitRootDirectory();
            if (string.IsNullOrEmpty(gitRoot))
                return null;

            string relativeFilePath = MakeRelativePath(gitRoot, assetPath);

            return GetCommit("--diff-filter=A", relativeFilePath, follow)?.FirstOrDefault();
        }

        private static string GetGitRootDirectory()
        {
            if (string.IsNullOrEmpty(GIT_ROOT_DIRECTORY))
            {
                RunGitCommand("rev-parse --show-toplevel", out string gitRoot);
                GIT_ROOT_DIRECTORY =  gitRoot?.TrimEnd('\n');
            }

            return GIT_ROOT_DIRECTORY;
        }

        public static string GetLastCommitMessage()
        {
            RunGitCommand("log -1 --pretty=%B", out string result);
            return result?.Trim();
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

        private static List<GitCommitInfo> GetCommit(string query, string fileName, bool follow = true)
        {
            string followFlag = follow ? " --follow" : string.Empty;
            string commandFormat = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows
                ? "log {0}{1} --format=\"%h|%an|%ae|%aI|%s\" -- \"{2}\""
                : "log {0}{1} --format='%h|%an|%ae|%aI|%s' -- '{2}'";

            string gitCommand = string.Format(commandFormat, query, followFlag, fileName);

            RunGitCommand(gitCommand, out string commandOutput);

            if (commandOutput == null)
                return null;

            string[] lines = commandOutput.Split('\n');

            List<GitCommitInfo> result = new List<GitCommitInfo>();

            foreach (string line in lines)
            {
                // The subject comes last so embedded '|' characters survive the split.
                string[] parts = line.Split(new[] { '|' }, 5);
                if (parts.Length < 5)
                    continue;

                // %aI is strict ISO 8601 with the author's UTC offset; shown in local time.
                DateTime dateTime = default;
                if (DateTimeOffset.TryParse(parts[3], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed))
                    dateTime = parsed.LocalDateTime;

                result.Add(new GitCommitInfo(parts[0], parts[1], parts[2], dateTime, parts[4]));
            }

            return result;
        }
    }
}
