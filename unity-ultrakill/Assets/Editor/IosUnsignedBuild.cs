using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class IosUnsignedBuild
{
    /// <summary>
    /// Used by game-ci / GitHub Actions: -executeMethod IosUnsignedBuild.Build
    /// </summary>
    public static void Build()
    {
        DisableBurstCompilation();
        EnsureIosPlayerSettings();

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            scenes = new[] { "Assets/Scenes/MainBoot.unity" };
        }

        var output = Path.Combine("build", "iOS");
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.iOS,
            options = BuildOptions.Development
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("iOS build failed: " + report.summary.result);
            EditorApplication.Exit(1);
        }

        Debug.Log("iOS Xcode project exported to " + output);
    }

    private const string IosBundleId = "com.ultrakill.ios.port";

    private static void EnsureIosPlayerSettings()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, IosBundleId);
        if (string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)))
        {
            Debug.LogError("iOS bundle identifier is empty after SetApplicationIdentifier.");
            EditorApplication.Exit(1);
        }

        Debug.Log("Using iOS bundle identifier: " + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS));
    }

    private static void DisableBurstCompilation()
    {
        var burst = System.Type.GetType("Unity.Burst.BurstCompiler, Unity.Burst");
        burst?.GetProperty("DisableCompilation", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, true);

        var burstEditor = System.Type.GetType("Unity.Burst.Editor.BurstCompiler, Unity.Burst.Editor");
        var options = burstEditor?.GetProperty("Options", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
        options?.GetType().GetProperty("EnableBurstCompilation")?.SetValue(options, false);
    }
}
