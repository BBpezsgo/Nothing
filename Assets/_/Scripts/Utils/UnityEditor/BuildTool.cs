using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

#nullable enable

namespace Utilities.Editor
{
    public class BuildTool : EditorWindow
    {
        readonly Dictionary<BuildTarget, TargetSettings> TargetsToBuild = new();
        readonly List<BuildTarget> AvailableTargets = new();

        public const string BaseOutputPath = "C:/Users/bazsi/Nothing 3D/Build/";

        bool IsCompressing;

        class TargetSettings
        {
            internal bool ProductionBuild;
            internal bool DevelopmentBuild;

            internal bool IsAnySelected => ProductionBuild || DevelopmentBuild;

            public TargetSettings()
            {
                ProductionBuild = false;
                DevelopmentBuild = false;
            }
        }

        [MenuItem("Tools/Build")]
        public static void OnShow()
        {
            EditorWindow.GetWindow<BuildTool>("Build Tools", true);
        }

        void OnEnable()
        {
            AvailableTargets.Clear();

            foreach (BuildTarget buildTarget in Enum.GetValues(typeof(BuildTarget)))
            {
                if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(buildTarget), buildTarget)) continue;
                if (!TargetsToBuild.ContainsKey(buildTarget))
                { TargetsToBuild.Add(buildTarget, new TargetSettings()); }

                AvailableTargets.Add(buildTarget);
            }

            if (TargetsToBuild.Count > AvailableTargets.Count)
            {
                List<BuildTarget> targetsToRemove = new();
                foreach (BuildTarget target in TargetsToBuild.Keys)
                {
                    if (!AvailableTargets.Contains(target))
                    { targetsToRemove.Add(target); }
                }

                foreach (BuildTarget target in targetsToRemove)
                { TargetsToBuild.Remove(target); }
            }
        }

        void OnGUI()
        {
            GUILayout.Label("Platforms to Build", EditorStyles.boldLabel);

            int numEnabled = 0;
            foreach (BuildTarget target in AvailableTargets)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(target.ToString(), EditorStyles.boldLabel);

                GUILayout.Label("Options", EditorStyles.boldLabel);
                TargetsToBuild[target].DevelopmentBuild = EditorGUILayout.Toggle("Development Build", TargetsToBuild[target].DevelopmentBuild);
                TargetsToBuild[target].ProductionBuild = EditorGUILayout.Toggle("Production Build", TargetsToBuild[target].ProductionBuild);

                if (TargetsToBuild[target].ProductionBuild) numEnabled++;
                if (TargetsToBuild[target].DevelopmentBuild) numEnabled++;

                GUILayout.EndVertical();
            }

            GUI.enabled = numEnabled > 0 && !IsCompressing;
            if (GUILayout.Button($"Build Selected Platforms ({numEnabled})"))
            {
                List<BuildTarget> selectedPlatforms = new();
                foreach (BuildTarget target in TargetsToBuild.Keys)
                {
                    if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target)) continue;
                    if (!TargetsToBuild[target].IsAnySelected) continue;
                    selectedPlatforms.Add(target);
                }

                EditorCoroutineUtility.StartCoroutine(PerformBuild(selectedPlatforms.ToArray()), this);
            }
            GUI.enabled = true;

            if (GUILayout.Button("Open Folder"))
            {
                Process.Start("explorer.exe", BaseOutputPath.Replace('/', '\\'));
            }

            GUI.enabled = !IsCompressing;
            if (GUILayout.Button("Compress All"))
            {
                List<string> folders = new();

                foreach (BuildTarget target in AvailableTargets)
                {
                    if (target == BuildTarget.Android) continue;

                    folders.Add($"{BaseOutputPath}{target}/");
                    folders.Add($"{BaseOutputPath}{target}-dev/");
                }

                EditorCoroutineUtility.StartCoroutine(PerformCompress(folders.ToArray()), this);
            }
            GUI.enabled = true;
        }

        static void Compress((string SourceDirectory, string DestinationZip) p)
        {
            if (!Directory.Exists(p.SourceDirectory)) return;
            if (File.Exists(p.DestinationZip))
            { File.Delete(p.DestinationZip); }
            ZipFile.CreateFromDirectory(p.SourceDirectory, p.DestinationZip, System.IO.Compression.CompressionLevel.Optimal, false);
        }

        IEnumerator PerformCompress(string[] folders)
        {
            IsCompressing = true;

            int progressId = Progress.Start("Compress", "Compressing all build output folders", Progress.Options.Sticky);
            Progress.ShowDetails();
            Progress.SetTimeDisplayMode(progressId, Progress.TimeDisplayMode.NoTimeShown);
            yield return null;

            bool isFailed = false;

            for (int i = 0; i < folders.Length; i++)
            {
                string path = folders[i];

                path = path.TrimEnd('/', '\\');
                string fileName = Path.GetFileName(path);

                yield return null;
                if (!Directory.Exists(path)) continue;

                IEnumerator task = CoroutineUtils.Task(ThreadTask.Start(Compress, (path, $"{BaseOutputPath}/{fileName}.zip")));
                while (task.MoveNext()) yield return null;

                Progress.Report(progressId, i + 1, folders.Length, $"{fileName}.zip");
                yield return null;
            }

            Progress.Finish(progressId, isFailed ? Progress.Status.Failed : Progress.Status.Succeeded);

            IsCompressing = false;
            yield return null;
        }

        IEnumerator PerformBuild(BuildTarget[] targets)
        {
            int buildAllProgressID = Progress.Start("Build", $"Build all selected platforms ({targets.Length})", Progress.Options.Sticky);
            Progress.ShowDetails();
            Progress.SetTimeDisplayMode(buildAllProgressID, Progress.TimeDisplayMode.NoTimeShown);
            yield return new EditorWaitForSeconds(1f);

            BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;

            bool anyBuildFailed = false;

            int totalSteps = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                if (TargetsToBuild[targets[i]].ProductionBuild) totalSteps++;
                if (TargetsToBuild[targets[i]].DevelopmentBuild) totalSteps++;
            }

            int currentStep = 0;

            for (int i = 0; i < targets.Length; i++)
            {
                BuildTarget target = targets[i];

                if (TargetsToBuild[target].ProductionBuild)
                {
                    int buildProgressID = Progress.Start($"Build {target}", null, Progress.Options.Sticky, buildAllProgressID);
                    Progress.SetStepLabel(buildAllProgressID, $"Build {target}");
                    yield return new EditorWaitForSeconds(1f);

                    BuildReport? buildResult = BuildIndividualTarget(target, false);

                    if (buildResult != null)
                    {
                        switch (buildResult.summary.result)
                        {
                            case BuildResult.Succeeded:
                                Progress.Finish(buildProgressID, Progress.Status.Succeeded);
                                break;
                            case BuildResult.Failed:
                                Progress.Finish(buildProgressID, Progress.Status.Failed);
                                anyBuildFailed = true;
                                break;
                            case BuildResult.Cancelled:
                                Progress.Finish(buildProgressID, Progress.Status.Canceled);
                                break;
                            default:
                                Progress.Finish(buildProgressID, Progress.Status.Failed);
                                anyBuildFailed = true;
                                break;
                        }
                    }
                    else
                    {
                        Progress.Finish(buildProgressID, Progress.Status.Failed);
                        anyBuildFailed = true;
                    }
                    currentStep++;
                }
                if (TargetsToBuild[target].DevelopmentBuild)
                {
                    int buildProgressID = Progress.Start($"Build Dev {target}", null, Progress.Options.Sticky, buildAllProgressID);
                    Progress.SetStepLabel(buildAllProgressID, $"Build Dev {target}");
                    yield return new EditorWaitForSeconds(1f);

                    BuildReport? buildResult = BuildIndividualTarget(target, true);

                    if (buildResult != null)
                    {
                        switch (buildResult.summary.result)
                        {
                            case BuildResult.Succeeded:
                                Progress.Finish(buildProgressID, Progress.Status.Succeeded);
                                break;
                            case BuildResult.Failed:
                                Progress.Finish(buildProgressID, Progress.Status.Failed);
                                anyBuildFailed = true;
                                break;
                            case BuildResult.Cancelled:
                                Progress.Finish(buildProgressID, Progress.Status.Canceled);
                                break;
                            default:
                                Progress.Finish(buildProgressID, Progress.Status.Failed);
                                anyBuildFailed = true;
                                break;
                        }
                    }
                    else
                    {
                        Progress.Finish(buildProgressID, Progress.Status.Failed);
                        anyBuildFailed = true;
                    }
                    currentStep++;
                }

                Progress.Report(buildAllProgressID, currentStep, totalSteps);
            }

            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildPipeline.GetBuildTargetGroup(originalTarget), originalTarget);

            Progress.Finish(buildAllProgressID, anyBuildFailed ? Progress.Status.Failed : Progress.Status.Succeeded);

            yield return null;
        }

        BuildReport? BuildIndividualTarget(BuildTarget target, bool developmentBuild)
        {
            TargetSettings userSettings = TargetsToBuild[target];

            BuildPlayerOptions options = new();

            IEnumerable<string> scenes = EditorBuildSettings.scenes.Select(scene => scene.path);
            options.scenes = scenes.ToArray();

            options.target = target;
            options.targetGroup = BuildPipeline.GetBuildTargetGroup(target);

            string path = BaseOutputPath + target.ToString() + (developmentBuild ? "-dev" : "") + "/";

            if (target == BuildTarget.Android)
            { options.locationPathName = path + PlayerSettings.productName + ".apk"; }
            else if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
            { options.locationPathName = path + PlayerSettings.productName + ".exe"; }
            else
            { options.locationPathName = path + PlayerSettings.productName; }

            if (BuildPipeline.BuildCanBeAppended(target, options.locationPathName) == CanAppendBuild.Yes)
            { options.options = BuildOptions.AcceptExternalModificationsToPlayer; }
            else
            { options.options = BuildOptions.None; }

            if (developmentBuild)
            {
                options.options |= BuildOptions.Development;
                if (target == BuildTarget.WebGL) options.options |= BuildOptions.ConnectWithProfiler;
            }

            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }

            return report;
        }

        static DirectoryInfo? SearchFolder(DirectoryInfo folder)
        {
            DirectoryInfo[] folders = folder.GetDirectories();
            FileInfo[] files = folder.GetFiles();
            if (files.Length > 0) return folder;
            if (folders.Length == 0) return null;
            if (folders.Length > 1) return folder;
            return SearchFolder(folders[0]);
        }
    }
}
