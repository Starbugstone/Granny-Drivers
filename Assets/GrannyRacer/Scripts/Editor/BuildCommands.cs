using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GrannyRacer.Editor
{
    /// <summary>
    /// Batch-mode build entry points, invoked by Tools/unity-build.ps1 via -executeMethod.
    /// </summary>
    /// <remarks>
    /// These methods call EditorApplication.Exit explicitly. Without that, Unity's -quit
    /// returns 0 even when the build reported a failure, and a broken build would look
    /// like a successful one to the calling script.
    /// </remarks>
    public static class BuildCommands
    {
        private const string ExecutableName = "GrannyRacer.exe";
        private const string OutputArgument = "-buildOutput";
        private const string DefaultOutputRoot = "Builds";

        [MenuItem("Granny Racer/Build/Windows (Development)")]
        public static void BuildWindowsDevelopment()
        {
            Run(BuildOptions.Development | BuildOptions.AllowDebugging, "Development");
        }

        [MenuItem("Granny Racer/Build/Windows (Release)")]
        public static void BuildWindowsRelease()
        {
            Run(BuildOptions.None, "Release");
        }

        private static void Run(BuildOptions options, string configuration)
        {
            var isBatchMode = Application.isBatchMode;

            try
            {
                var outputDirectory = ResolveOutputDirectory(configuration);
                Directory.CreateDirectory(outputDirectory);

                var scenes = GetScenePaths();
                if (scenes.Length == 0)
                {
                    Fail(isBatchMode,
                        "No scenes to build. Add at least one scene to " +
                        "File > Build Profiles (Scene List), or place a scene under Assets/.");
                    return;
                }

                var locationPath = Path.Combine(outputDirectory, ExecutableName);

                Debug.Log($"[Build] configuration={configuration}");
                Debug.Log($"[Build] output={locationPath}");
                Debug.Log($"[Build] scenes=\n  {string.Join("\n  ", scenes)}");

                var playerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = locationPath,
                    target = BuildTarget.StandaloneWindows64,
                    targetGroup = BuildTargetGroup.Standalone,
                    options = options,
                };

                var report = BuildPipeline.BuildPlayer(playerOptions);
                var summary = report.summary;

                Debug.Log($"[Build] result={summary.result} " +
                          $"errors={summary.totalErrors} warnings={summary.totalWarnings} " +
                          $"size={summary.totalSize / (1024 * 1024)}MB " +
                          $"duration={summary.totalTime}");

                if (summary.result != BuildResult.Succeeded)
                {
                    LogFailedSteps(report);
                    Fail(isBatchMode, $"Build failed with result '{summary.result}'.");
                    return;
                }

                if (!File.Exists(locationPath))
                {
                    Fail(isBatchMode,
                        $"Build reported success but '{locationPath}' does not exist.");
                    return;
                }

                Debug.Log("[Build] OK");
                if (isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Build] Unhandled exception: {exception}");
                Fail(isBatchMode, exception.Message);
            }
        }

        /// <summary>
        /// Reads -buildOutput from the command line, falling back to Builds/Windows-{configuration}.
        /// </summary>
        private static string ResolveOutputDirectory(string configuration)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], OutputArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            return Path.Combine(projectRoot, DefaultOutputRoot, $"Windows-{configuration}");
        }

        /// <summary>
        /// Enabled scenes from the build profile. If none are configured, falls back to
        /// every scene under Assets/ so a fresh clone can still produce a build.
        /// </summary>
        private static string[] GetScenePaths()
        {
            var configured = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            if (configured.Length > 0)
            {
                return configured;
            }

            Debug.LogWarning(
                "[Build] No scenes enabled in the build profile. " +
                "Falling back to every scene found under Assets/. " +
                "Configure the scene list before producing a release build.");

            return AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static void LogFailedSteps(BuildReport report)
        {
            var messages = new List<string>();
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        messages.Add($"[{step.name}] {message.content}");
                    }
                }
            }

            if (messages.Count > 0)
            {
                Debug.LogError("[Build] Failures:\n" + string.Join("\n", messages));
            }
        }

        private static void Fail(bool isBatchMode, string message)
        {
            Debug.LogError($"[Build] {message}");
            if (isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
