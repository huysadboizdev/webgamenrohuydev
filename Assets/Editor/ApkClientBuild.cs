using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ApkClientBuild
{
    public const string ProductName = "NROBEAR";
    public const string BundleId = "com.nrobear.client";

    // Manual entry point only. Source installation never invokes this method.
    // Builds the Android client (debug-signed). Does not touch WebGL settings or output.
    [MenuItem("NRO PC/Build Android APK client")]
    public static void Run()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 2 || !scenes.Contains("Assets/Scenes/NROL.unity") || !scenes.Contains("Assets/Scenes/NROL1.unity"))
            throw new Exception("PRJ requires both native account scenes");
        string configured = Environment.GetEnvironmentVariable("NRO_APK_BUILD_OUTPUT");
        string output = Path.GetFullPath(string.IsNullOrEmpty(configured)
            ? Path.Combine(Application.dataPath, "../Builds/APK/NROBEAR.apk") : configured);
        PlayerSettings.productName = ProductName;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleId);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PcClientBuild.SetAppIcon(BuildTargetGroup.Android);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes, locationPathName = output, target = BuildTarget.Android, options = BuildOptions.None
        });
        string result = "Result=" + report.summary.result + "\nErrors=" + report.summary.totalErrors + "\nWarnings=" + report.summary.totalWarnings;
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build-result.txt"), result);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception(result);
    }
}
