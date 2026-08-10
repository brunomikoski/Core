using System;
using BrunoMikoski.Core.DevelopmentTools.Git;
#if PARRELSYNC_AVAILABLE
using ParrelSync;
#endif
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Builds
{
    [InitializeOnLoad]
    public static class AutoGitCommitReader
    {
        private static bool warnedUnresolvable;

        static AutoGitCommitReader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.focusChanged += OnEditorFocusChanged;
            EditorApplication.delayCall += () => StoreCurrentGitCommit();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            StoreCurrentGitCommit();
        }

        private static void OnEditorFocusChanged(bool focused)
        {
            if (!focused)
                return;

            StoreCurrentGitCommit();
        }

        [MenuItem("Tools/Git/Store Current Git Commit")]
        public static bool StoreCurrentGitCommit()
        {
            if (!TryResolveHeadSha(out string sha))
            {
                if (!warnedUnresolvable)
                {
                    warnedUnresolvable = true;
                    Debug.LogWarning($"[{nameof(AutoGitCommitReader)}] This checkout has no resolvable .git; the stored git commit bridge stays '{GitUtility.StoredGitCommitBridge}'.");
                }

                return false;
            }

            if (string.Equals(GitUtility.StoredGitCommitBridge, sha, StringComparison.Ordinal))
                return true;

            GitUtility.CurrentGitCommit = sha;

            Debug.Log($"[{nameof(AutoGitCommitReader)}] Updated stored git commit to {sha}");
            return true;
        }

        private static bool TryResolveHeadSha(out string sha)
        {
            if (GitUtility.TryGetRepositoryHeadSha(out sha))
                return true;

#if PARRELSYNC_AVAILABLE

            if (!ClonesManager.IsClone())
                return false;
            return GitUtility.TryResolveGitDirectory(ClonesManager.GetOriginalProjectPath(), out string gitDirectory)
                   && GitUtility.TryReadHeadSha(gitDirectory, out sha, out _);
#endif
            return false;
        }
    }
}
