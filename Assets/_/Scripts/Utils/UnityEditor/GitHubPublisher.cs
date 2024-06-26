using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Octokit;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

#nullable enable

namespace Utilities.Editor
{
    public class GitHubReleaser : EditorWindow
    {
        static readonly GitHubClient GitHubClient = new(new ProductHeaderValue("nothing"));
        static readonly List<Task> GitHubTasks = new();
        static ApiInfo? LastApiInfo;

        [MenuItem("Tools/GitHub Publishing")]
        public static void OnShow()
        {
            EditorWindow.GetWindow<GitHubReleaser>("GitHub Publishing", true);
        }

        void OnGUI()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Files to publish", EditorStyles.boldLabel);

            foreach (string? localFile in Directory.GetFiles(BuildTool.BaseOutputPath, "*.zip", SearchOption.TopDirectoryOnly))
            {
                FileInfo file = new(localFile);

                GUILayout.BeginHorizontal();

                GUILayout.Label(file.Name);
                GUILayout.Label($"({file.LastWriteTime})");

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Github", EditorStyles.boldLabel);

            GitHubClient.Credentials = new Credentials(GitHubCredentials.ApiKey);

            if (LastApiInfo != null)
            { GUILayout.Label($"Rate Limit: {LastApiInfo.RateLimit.Remaining}/{LastApiInfo.RateLimit.Limit}"); }

            for (int i = GitHubTasks.Count - 1; i >= 0; i--)
            {
                if (GitHubTasks[i].IsCompleted)
                { GitHubTasks.RemoveAt(i); }
            }

            using (GUIUtils.Enabled(GitHubTasks.Count == 0))
            {
                if (GUILayout.Button("Publish") && GitHubTasks.Count == 0)
                {
                    PublishToGitHubAsync();
                }
            }

            GUILayout.EndVertical();
        }

        static async Task<T?> SendRequest<T>(Task<T> task)
        {
            GitHubTasks.Add(task);
            T? result;
            try
            { result = await task; }
            catch (NotFoundException notFoundException)
            {
                Debug.LogWarning($"[{nameof(GitHubReleaser)}]: ({typeof(T).Name}) HTTP {(int)notFoundException.HttpResponse.StatusCode} ({notFoundException.HttpResponse.StatusCode}): {notFoundException.HttpResponse.Body}");
                result = default;
            }
            LastApiInfo = GitHubClient.GetLastApiInfo();
            return result;
        }

        public static async void PublishToGitHubAsync()
        {
            using ProgressAuto progress = new("Publish to GitHub", options: Progress.Options.Sticky);
            const int TotalSteps = 5;

            string[] localFiles = Directory.GetFiles(BuildTool.BaseOutputPath, "*.zip", SearchOption.TopDirectoryOnly);

            Debug.Log($"[{nameof(GitHubReleaser)}]: Getting the last commit");
            progress.Report(1, TotalSteps, "Getting the last commit");

            IReadOnlyList<GitHubCommit>? commits = await SendRequest(GitHubClient.Repository.Commit.GetAll(GitHubCredentials.UserName, GitHubCredentials.Repository, new ApiOptions()
            {
                PageCount = 1,
                PageSize = 1,
                StartPage = 0,
            }));

            if (commits is null || commits.Count == 0)
            {
                Debug.LogError($"[{nameof(GitHubReleaser)}]: No commits");
                progress.StepLabel = "No commits";
                progress.Finish(Progress.Status.Failed);
                return;
            }

            GitHubCommit commit = commits[0];

            Debug.Log($"[{nameof(GitHubReleaser)}]: Getting the latest tag");
            progress.Report(2, TotalSteps, "Getting the latest tag");
            GitTag? tag = await SendRequest(GitHubClient.Git.Tag.Get(GitHubCredentials.UserName, GitHubCredentials.Repository, commit.Sha));

            if (tag is null)
            {
                Debug.Log($"[{nameof(GitHubReleaser)}]: Creating new tag");
                progress.Report(3, TotalSteps, "Creating new tag");
                tag = await SendRequest(GitHubClient.Git.Tag.Create(GitHubCredentials.UserName, GitHubCredentials.Repository, new NewTag()
                {
                    Message = "Bruh",
                    Tag = $"{DateTime.UtcNow:yyyy-MM-dd}",
                    Object = commit.Sha,
                    Type = TaggedType.Commit,
                    Tagger = new Committer(GitHubCredentials.UserName, GitHubCredentials.Email, DateTimeOffset.UtcNow),
                }));

                if (tag is null)
                {
                    Debug.LogError($"[{nameof(GitHubReleaser)}]: Failed to create new tag");
                    progress.StepLabel = "Failed to create new tag";
                    progress.Finish(Progress.Status.Failed);
                    return;
                }
            }

            Debug.Log($"[{nameof(GitHubReleaser)}]: Getting the latest release");
            progress.Report(4, TotalSteps, "Getting the latest release");
            Release? release = await SendRequest(GitHubClient.Repository.Release.Get(GitHubCredentials.UserName, GitHubCredentials.Repository, tag.Tag));

            if (release is not null)
            {
                Debug.Log($"[{nameof(GitHubReleaser)}]: Release already exists");
                progress.StepLabel = "Release already exists";
                progress.Finish(Progress.Status.Succeeded);
                return;
            }

            Debug.Log($"[{nameof(GitHubReleaser)}]: Creating release");
            progress.Report(5, TotalSteps, "Creating release");

            release = await SendRequest(GitHubClient.Repository.Release.Create(GitHubCredentials.UserName, GitHubCredentials.Repository, new NewRelease(tag.Tag)
            {
                Name = $"Release {tag.Tag}",
                Body = "Yeah",
                Draft = false,
                Prerelease = true,
                MakeLatest = MakeLatestQualifier.True,
            }));

            if (release is null)
            {
                Debug.LogError($"[{nameof(GitHubReleaser)}]: Failed to create release");
                progress.StepLabel = "Failed to create release";
                progress.Finish(Progress.Status.Failed);
                return;
            }

            using (ProgressAuto uploadAssetsProgress = new("Upload assets", parentId: progress.Id))
            {
                bool isSuccess = true;

                for (int i = 0; i < localFiles.Length; i++)
                {
                    FileInfo file = new(localFiles[i]);

                    uploadAssetsProgress.Report(i + 1, localFiles.Length, file.Name);

                    await using FileStream stream = File.OpenRead(file.FullName);
                    ReleaseAsset? releaseAsset = await SendRequest(GitHubClient.Repository.Release.UploadAsset(release, new ReleaseAssetUpload(file.Name, "application/x-zip", stream, null)));

                    if (releaseAsset is null)
                    {
                        Debug.LogError($"[{nameof(GitHubReleaser)}]: Failed to upload asset {file.Name}");
                        isSuccess = false;
                        continue;
                    }

                    Debug.LogError($"[{nameof(GitHubReleaser)}]: Asset {file.Name} uploaded");
                }

                uploadAssetsProgress.Finish(isSuccess ? Progress.Status.Succeeded : Progress.Status.Failed);
            }

            Debug.Log($"[{nameof(GitHubReleaser)}]: Release created: {release.HtmlUrl}");
            progress.StepLabel = "Release created";
            progress.Finish(Progress.Status.Succeeded);
        }
    }
}
