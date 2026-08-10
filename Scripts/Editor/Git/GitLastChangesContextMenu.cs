using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Git
{
    public static class GitLastChangesContextMenu
    {
        private const string MenuPath = "Assets/Show Last Changes Information";
        private const int MaxBranchesShown = 8;
        private const string ShaColor = "#4EC9B0";
        private const string WarningColor = "#FFC46B";

        [MenuItem(MenuPath, true)]
        private static bool ValidateShowLastChangesInformation()
        {
            return Selection.assetGUIDs != null && Selection.assetGUIDs.Length == 1;
        }

        [MenuItem(MenuPath, false, 2000)]
        private static void ShowLastChangesInformation()
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
            if (string.IsNullOrEmpty(assetPath))
                return;

            if (!GitUtility.IsGitInstalled())
            {
                Debug.LogWarning("[Git] Git was not found. Set the executable under Preferences > Git Utility, or run Tools/Setup Git Tools.");
                return;
            }

            GitCommitInfo lastCommit = null;
            List<string> branches = null;
            bool hasLocalChanges = false;

            try
            {
                EditorUtility.DisplayProgressBar("Git", $"Reading history of {assetPath}…", 0.5f);

                lastCommit = GitUtility.GetLastCommit(assetPath, follow: !AssetDatabase.IsValidFolder(assetPath));
                hasLocalChanges = GitUtility.HasUncommittedChanges(assetPath);

                if (lastCommit != null)
                    branches = GitUtility.GetBranchesContaining(lastCommit.sha);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            LogLastChanges(assetPath, lastCommit, branches, hasLocalChanges);
        }

        private static void LogLastChanges(string assetPath, GitCommitInfo lastCommit, List<string> branches, bool hasLocalChanges)
        {
            UnityEngine.Object context = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (lastCommit == null)
            {
                Debug.LogWarning($"<b>{assetPath}</b> has no commit history." +
                                 (hasLocalChanges ? " It has uncommitted local changes — probably new." : string.Empty), context);
                return;
            }

            string branchesLine;
            if (branches == null || branches.Count == 0)
            {
                branchesLine = "not reachable from any local or remote branch";
            }
            else
            {
                int shownCount = Math.Min(branches.Count, MaxBranchesShown);
                branchesLine = string.Join(", ", branches.GetRange(0, shownCount));
                if (branches.Count > shownCount)
                    branchesLine += $" (+{branches.Count - shownCount} more)";
            }

            string message =
                $"<b>{Path.GetFileName(assetPath)}</b> — last changed by <b>{lastCommit.author}</b>, {ToRelativeTime(lastCommit.dateTime)}\n" +
                "\n" +
                $"<b>Asset:</b> {assetPath}\n" +
                $"<b>Commit:</b> <color={ShaColor}>{lastCommit.sha}</color> — {lastCommit.title}\n" +
                $"<b>Author:</b> {lastCommit.author} ({lastCommit.email})\n" +
                $"<b>Date:</b> {lastCommit.dateTime:yyyy-MM-dd HH:mm} ({ToRelativeTime(lastCommit.dateTime)})\n" +
                $"<b>Branches:</b> {branchesLine}";

            if (hasLocalChanges)
                message += $"\n<color={WarningColor}>This asset also has uncommitted local changes on this machine.</color>";

            Debug.Log(message, context);
        }

        private static string ToRelativeTime(DateTime dateTime)
        {
            if (dateTime == default)
                return "unknown";

            TimeSpan age = DateTime.Now - dateTime;
            if (age.TotalMinutes < 1d)
                return "just now";
            if (age.TotalHours < 1d)
                return $"{(int)age.TotalMinutes}m ago";
            if (age.TotalDays < 1d)
                return $"{(int)age.TotalHours}h ago";
            if (age.TotalDays < 60d)
                return $"{(int)age.TotalDays}d ago";

            return $"{(int)(age.TotalDays / 30d)} months ago";
        }
    }
}
