using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PaperHouse.GitTools.Editor
{
    public static class GitTools
    {
        private const int MaxWaitTime = 1000;

        #region Editor Menu items

        [MenuItem("Paper House/Tools/Git/Print Git Tool Path")]
        private static void PrintGitToolPath()
        {
            string gitToolPath = GetGitToolPath();
            Debug.Log($"[Git.Tools] using git installed at path: {gitToolPath}");
        }

        [MenuItem("Paper House/Tools/Git/Print Git Repository Path")]
        private static void PrintGitRepositoryInParentDirectory()
        {
            string searchPath = Application.dataPath;
            Debug.Log($"[Git.Tools] Searching for Git repository from: {searchPath}");

            string gitRepositoryPath = FindGitRepositoryInParentDirectory(searchPath);

            if (!string.IsNullOrEmpty(gitRepositoryPath))
                Debug.Log($"[Git.Tools] Found Git Repository: {gitRepositoryPath}");
        }

        #endregion

        #region Environment Methods

        private static string GetGitToolPath()
        {
            // Setup Command
            Process prc = new();
            prc.StartInfo.FileName = "where";
            prc.StartInfo.Arguments = "git";

            // Run Command
            prc.StartInfo.RedirectStandardOutput = true;
            prc.StartInfo.UseShellExecute = false;
            prc.Start();
            prc.WaitForExit(MaxWaitTime);

            // Clean output & return path
            string rawOutput = prc.StandardOutput.ReadLine();
            if (rawOutput != null)
            {
                string path = rawOutput.Replace("\n", string.Empty);
                return path;
            }

            // Print failure and return empty.
            Debug.Log("[Git.Tools] Failed to find git executable path.");
            return string.Empty;
        }

        public static string FindGitRepositoryInParentDirectory(string buildPath)
        {
            // Strip back buildpath to first directory if needed. (Builds\Output\Game.exe -> Builds\Output\)
            string basePath = Directory.Exists(buildPath) ? buildPath : Directory.GetParent(buildPath)!.FullName;

            const string targetFolder = ".git";
            bool isRepoFound = false;
            bool searchFailed = false;
            string searchPath = basePath;
            string output = string.Empty;

            // While we've not located the target folder, keep looping up the parent directory until it is found.
            while (!isRepoFound)
            {
                // Move up to next parent directory
                DirectoryInfo nextSearchPath = Directory.GetParent(searchPath);

                // If next search directory failed, we run out of parent directories to search, break out of the search loop.
                if (nextSearchPath == null)
                {
                    searchFailed = true;
                    break;
                }

                // Set new search directory
                searchPath = nextSearchPath?.FullName;

                // Look over each child directory of search path and see any of them match our target.
                string[] subDirectories = Directory.GetDirectories(searchPath);
                foreach (string subDir in subDirectories)
                {
                    if (!subDir.Contains(targetFolder))
                        continue;

                    isRepoFound = true;
                    output = subDir;
                    Debug.Log($"[Git.Tools] Found Git Directory: {subDir}");
                }
            }

            if (searchFailed) Debug.Log($"[Git.Tools] Failed to find git repository from path '{buildPath}'");

            return output;
        }

        #endregion

        public static string GetCurrentCommitHash(string repoPath, bool isShort)
        {
            Process prc = new();

            prc.StartInfo.FileName = GetGitToolPath();
            prc.StartInfo.WorkingDirectory = repoPath;

            if (isShort) prc.StartInfo.Arguments = "rev-parse --short HEAD";
            else prc.StartInfo.Arguments = "show-ref --hash";

            prc.StartInfo.RedirectStandardOutput = true;
            prc.StartInfo.UseShellExecute = false;
            prc.Start();
            prc.WaitForExit(MaxWaitTime);

            string data = prc.StandardOutput.ReadToEnd();
            int i = data.IndexOf("\n", StringComparison.InvariantCulture);

            return i > 0 ? data.Substring(0, i) : data;
        }

        public static string GetCommitCount(string repoPath)
        {
            Process prc = new();

            prc.StartInfo.FileName = GetGitToolPath();
            prc.StartInfo.WorkingDirectory = repoPath;
            prc.StartInfo.Arguments = $"rev-list HEAD --count";

            prc.StartInfo.RedirectStandardOutput = true;
            prc.StartInfo.UseShellExecute = false;
            prc.Start();
            prc.WaitForExit(MaxWaitTime);

            string data = prc.StandardOutput.ReadToEnd();
            int i = data.IndexOf("\n", StringComparison.InvariantCulture);

            if (i > 0) return data.Substring(0, i);

            return data;
        }
    }

}
