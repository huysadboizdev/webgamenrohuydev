using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebClientBuild
{
    // Manual entry point only. Source installation never invokes this method.
    [MenuItem("NRO Web/Build PRJ two-tab browser client")]
    public static void Run()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 2 || !scenes.Contains("Assets/Scenes/NROL.unity") || !scenes.Contains("Assets/Scenes/NROL1.unity"))
            throw new Exception("PRJ requires both native account scenes");
        string configured = Environment.GetEnvironmentVariable("NRO_WEB_BUILD_OUTPUT");
        string output = Path.GetFullPath(string.IsNullOrEmpty(configured)
            ? Path.Combine(Application.dataPath, "../Builds/WebGL") : configured);
        PlayerSettings.WebGL.template = "PROJECT:NroWeb";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.initialMemorySize = 128;
        PlayerSettings.WebGL.maximumMemorySize = 1024;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithoutStacktrace;
        PlayerSettings.runInBackground = true;
        PlayerSettings.companyName = "NRO";
        PlayerSettings.productName = "NRO Two Tab Web";
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
        Directory.CreateDirectory(output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes, locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None
        });
        string result = "Result=" + report.summary.result + "\nErrors=" + report.summary.totalErrors + "\nWarnings=" + report.summary.totalWarnings;
        File.WriteAllText(Path.Combine(output, "build-result.txt"), result);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception(result);
    }
}
