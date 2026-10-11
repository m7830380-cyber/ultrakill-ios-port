using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Builds the iOS player "shell": an IL2CPP player that contains the (patched) retail ULTRAKILL
// assemblies. The game's own data files are swapped into the .app afterwards by tools/package_ipa.py.
public static class IosShellBuild
{
    const string ScenePath = "Assets/Scenes/Boot.unity";

    public static void Build()
    {
        string outDir = Environment.GetEnvironmentVariable("SHELL_OUT") ?? "Build/iOS";

        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

        PlayerSettings.companyName = "Hakita";
        PlayerSettings.productName = "ULTRAKILL";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.hakita.ultrakill.ios");
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ?? "1";
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1); // ARM64
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.iOS, ManagedStrippingLevel.Minimal);
        PlayerSettings.stripEngineCode = false;
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.iOS, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.iOS, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, ApiCompatibilityLevel.NET_Unity_4_8);
        PlayerSettings.allowUnsafeCode = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.iOS, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new[] { GraphicsDeviceType.Metal });
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        PlayerSettings.gpuSkinning = true;
        PlayerSettings.iOS.targetOSVersionString = "14.0";
        PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        PlayerSettings.iOS.requiresFullScreen = true;
        PlayerSettings.iOS.hideHomeButton = true;
        PlayerSettings.iOS.deferSystemGesturesMode = UnityEngine.iOS.SystemGestureDeferMode.All;
        PlayerSettings.iOS.statusBarStyle = iOSStatusBarStyle.Default;
        PlayerSettings.statusBarHidden = true;
        PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Suspend;
        PlayerSettings.accelerometerFrequency = 0;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.enableInternalProfiler = false;
        PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
        PlayerSettings.usePlayerLog = true;
        PlayerSettings.iOS.appleEnableAutomaticSigning = false;

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = outDir,
            target = BuildTarget.iOS,
            targetGroup = BuildTargetGroup.iOS,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[IosShellBuild] result: " + report.summary.result + " errors=" + report.summary.totalErrors);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }

    [PostProcessBuild(100)]
    public static void OnPostprocess(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        var root = plist.root;
        root.SetString("CFBundleDisplayName", "ULTRAKILL");
        root.SetBoolean("UIFileSharingEnabled", true);           // Documents visible in Files / Finder
        root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);
        root.SetBoolean("UISupportsDocumentBrowser", true);
        root.SetBoolean("GCSupportsControllerUserInteraction", true);
        root.SetBoolean("GCSupportsGameMode", true);
        root.SetBoolean("CADisableMinimumFrameDurationOnPhone", true); // allow 120Hz on ProMotion
        root.SetBoolean("UIRequiresFullScreen", true);
        root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        var gcProfiles = root.CreateArray("GCSupportedGameControllers");
        foreach (var p in new[] { "ExtendedGamepad" })
            gcProfiles.AddDict().SetString("ProfileName", p);
        plist.WriteToFile(plistPath);

        // Unsigned build for TrollStore: strip signing requirements from the Xcode project.
        string projPath = PBXProject.GetPBXProjectPath(path);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);
        foreach (var guid in new[] { proj.GetUnityMainTargetGuid(), proj.GetUnityFrameworkTargetGuid() })
        {
            proj.SetBuildProperty(guid, "CODE_SIGNING_ALLOWED", "NO");
            proj.SetBuildProperty(guid, "CODE_SIGNING_REQUIRED", "NO");
            proj.SetBuildProperty(guid, "CODE_SIGN_IDENTITY", "");
            proj.SetBuildProperty(guid, "ENABLE_BITCODE", "NO");
        }
        proj.WriteToFile(projPath);
    }
}
