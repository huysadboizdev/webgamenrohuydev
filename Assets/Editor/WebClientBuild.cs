using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebClientBuild
{
    [MenuItem("NRO Web/Build browser client")]
    public static void Run()
    {
        NpcImageDecodeCases.Run();
        WebTransportRegression.Run();
        WebStorageCases.Run();
        WebViewportRegression.Run();
        WebEffectDataCases.Run();
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/WebGL"));
        Directory.CreateDirectory(output);
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
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 2) throw new Exception("Both NROL and NROL1 must be enabled");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes, locationPathName = output, target = BuildTarget.WebGL, options = BuildOptions.None
        });
        string result = "Result="+report.summary.result+"\nErrors="+report.summary.totalErrors+"\nWarnings="+report.summary.totalWarnings+"\nBytes="+report.summary.totalSize+"\n";
        File.WriteAllText(Path.Combine(output,"build-result.txt"),result);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("WebGL build failed: "+result);
        Debug.Log("NRO_WEB_BUILD_SUCCEEDED " + output);
    }
}
