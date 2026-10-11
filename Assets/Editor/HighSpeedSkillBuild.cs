using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class HighSpeedSkillBuild
{
    public static void Run()
    {
        HighSpeedSkillRegression.Run();
        string output = Environment.GetEnvironmentVariable("SKILL_SPEED_BUILD_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new Exception("SKILL_SPEED_BUILD_OUTPUT is required");
        Directory.CreateDirectory(output);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 2) throw new Exception("Both client tab scenes must be enabled");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(output, "NRO-SkillSpeed.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        File.WriteAllText(Path.Combine(output, "build-result.txt"),
            "Result=" + report.summary.result + "\nErrors=" + report.summary.totalErrors
            + "\nWarnings=" + report.summary.totalWarnings
            + "\nTests=Actual Game1/Game2 Char/Controller/Service, fake monotonic clock and packet capture, speeds 1/2/5/10/20/50/100\n");
        if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
            throw new Exception("Skill client build failed: " + report.summary.result);
        Debug.Log("HIGH_SPEED_SKILL_BUILD_SUCCEEDED " + output);
    }
}
