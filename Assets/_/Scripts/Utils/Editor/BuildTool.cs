using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unity.EditorCoroutines.Editor;

using UnityEditor;
using UnityEditor.Build.Reporting;

using UnityEngine;

namespace Utilities.Editor
{
    public class BuildTool : EditorWindow
    {
        readonly Dictionary<BuildTarget, TargetSettings> TargetsToBuild = new();
        readonly List<BuildTarget> AvailableTargets = new();

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

            var buildTargets = System.Enum.GetValues(typeof(BuildTarget));
            foreach (var buildTargetValue in buildTargets)
            {
                BuildTarget buildTarget = (BuildTarget)buildTargetValue;

                if (!BuildPipeline.IsBuildTargetSupported(GetTargetGroup(buildTarget), buildTarget)) continue;
                if (!TargetsToBuild.ContainsKey(buildTarget))
                { TargetsToBuild.Add(buildTarget, new TargetSettings()); }

                AvailableTargets.Add(buildTarget);
            }

            if (TargetsToBuild.Count > AvailableTargets.Count)
            {
                List<BuildTarget> targetsToRemove = new();
                foreach (var target in TargetsToBuild.Keys)
                {
                    if (!AvailableTargets.Contains(target))
                    { targetsToRemove.Add(target); }
                }

                foreach (var target in targetsToRemove)
                { TargetsToBuild.Remove(target); }
            }
        }

        static BuildTargetGroup GetTargetGroup(BuildTarget buildTarget) => BuildPipeline.GetBuildTargetGroup(buildTarget);  /*buildTarget switch
    {
        BuildTarget.StandaloneOSX => BuildTargetGroup.Standalone,
        BuildTarget.StandaloneWindows => BuildTargetGroup.Standalone,
        BuildTarget.iOS => BuildTargetGroup.iOS,
        BuildTarget.Android => BuildTargetGroup.Android,
        BuildTarget.StandaloneWindows64 => BuildTargetGroup.Standalone,
        BuildTarget.WebGL => BuildTargetGroup.WebGL,
        BuildTarget.WSAPlayer => BuildTargetGroup.WSA,
        BuildTarget.StandaloneLinux64 => BuildTargetGroup.Standalone,
        BuildTarget.XboxOne => BuildTargetGroup.XboxOne,
        BuildTarget.tvOS => BuildTargetGroup.tvOS,
        BuildTarget.Switch => BuildTargetGroup.Switch,
        BuildTarget.Lumin => BuildTargetGroup.Lumin,
        BuildTarget.Stadia => BuildTargetGroup.Stadia,
        BuildTarget.GameCoreXboxOne => BuildTargetGroup.GameCoreXboxOne,
        BuildTarget.PS5 => BuildTargetGroup.PS5,
        BuildTarget.EmbeddedLinux => BuildTargetGroup.EmbeddedLinux,
        _ => BuildTargetGroup.Unknown,
    };*/

        void OnGUI()
        {
            GUILayout.Label($"Platforms to Build", EditorStyles.boldLabel);

            int numEnabled = 0;
            {
                foreach (var target in AvailableTargets)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.Label(target.ToString(), EditorStyles.boldLabel);

                    GUILayout.Label("Options", EditorStyles.boldLabel);
                    TargetsToBuild[target].DevelopmentBuild = EditorGUILayout.Toggle("Developer Build", TargetsToBuild[target].DevelopmentBuild);
                    TargetsToBuild[target].ProductionBuild = EditorGUILayout.Toggle("Production Build", TargetsToBuild[target].ProductionBuild);

                    if (TargetsToBuild[target].ProductionBuild) numEnabled++;
                    if (TargetsToBuild[target].DevelopmentBuild) numEnabled++;

                    GUILayout.EndVertical();
                }
            }

            GUI.enabled = numEnabled > 0;
            if (GUILayout.Button($"Build Selected {numEnabled} Platforms"))
            {
                List<BuildTarget> selectedPlatforms = new();
                foreach (var target in TargetsToBuild.Keys)
                {
                    if (!BuildPipeline.IsBuildTargetSupported(GetTargetGroup(target), target)) continue;
                    if (!TargetsToBuild[target].IsAnySelected) continue;
                    selectedPlatforms.Add(target);
                }

                EditorCoroutineUtility.StartCoroutine(PerformBuild(selectedPlatforms.ToArray()), this);
            }
            GUI.enabled = true;
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

                    BuildReport buildResult = BuildIndividualTarget(target, false);

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
                        case BuildResult.Unknown:
                        default:
                            Progress.Finish(buildProgressID, Progress.Status.Failed);
                            anyBuildFailed = true;
                            break;
                    }
                    currentStep++;
                }
                if (TargetsToBuild[target].DevelopmentBuild)
                {
                    int buildProgressID = Progress.Start($"Build Dev {target}", null, Progress.Options.Sticky, buildAllProgressID);
                    Progress.SetStepLabel(buildAllProgressID, $"Build Dev {target}");
                    yield return new EditorWaitForSeconds(1f);

                    BuildReport buildResult = BuildIndividualTarget(target, true);

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
                        case BuildResult.Unknown:
                        default:
                            Progress.Finish(buildProgressID, Progress.Status.Failed);
                            anyBuildFailed = true;
                            break;
                    }
                    currentStep++;
                }

                Progress.Report(buildAllProgressID, currentStep, totalSteps);
            }

            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(GetTargetGroup(originalTarget), originalTarget);

            Progress.Finish(buildAllProgressID, anyBuildFailed ? Progress.Status.Failed : Progress.Status.Succeeded);

            yield return null;
        }

        const string BaseOutputPath = "C:/Users/bazsi/Nothing 3D/Build/";

        BuildReport BuildIndividualTarget(BuildTarget target, bool developmentBuild)
        {
            var userSettings = TargetsToBuild[target];

            BuildPlayerOptions options = new();

            var scenes = EditorBuildSettings.scenes.Select(scene => scene.path);
            options.scenes = scenes.ToArray();

            options.target = target;
            options.targetGroup = GetTargetGroup(target);

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
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }

            return report;
        }

        static DirectoryInfo SearchFolder(DirectoryInfo folder)
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
