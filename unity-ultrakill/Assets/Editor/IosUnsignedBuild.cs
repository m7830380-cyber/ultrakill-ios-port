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
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            scenes = new[] { "Assets/Scenes/Bootstrap.unity" };
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
}
