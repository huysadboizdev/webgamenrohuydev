using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PcClientBuild
{
    // Manual entry point only. Source installation never invokes this method.
    // Builds the local Windows standalone client. Does not touch WebGL settings or output.
    [MenuItem("NRO PC/Build local two-tab PC client")]
    public static void Run()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 2 || !scenes.Contains("Assets/Scenes/NROL.unity") || !scenes.Contains("Assets/Scenes/NROL1.unity"))
            throw new Exception("PRJ requires both native account scenes");
        string configured = Environment.GetEnvironmentVariable("NRO_PC_BUILD_OUTPUT");
        string output = Path.GetFullPath(string.IsNullOrEmpty(configured)
            ? Path.Combine(Application.dataPath, "../Builds/PC/NRO-TwoTab-PC.exe") : configured);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        });
        string result = "Result=" + report.summary.result + "\nErrors=" + report.summary.totalErrors + "\nWarnings=" + report.summary.totalWarnings;
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(output), "build-result.txt"), result);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception(result);
    }
}
