using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ClientWebBuildTrigger
{
    private static bool started;
    static ClientWebBuildTrigger() { EditorApplication.update += Check; }
    private static void Check()
    {
        if (started || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string marker = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/.run-local-build"));
        if (!File.Exists(marker)) return;
        started = true;
        string output = File.ReadAllText(marker).Trim();
        File.Delete(marker);
        string errorFile = Path.Combine(Path.GetDirectoryName(marker), "unity-combat-death-build-error.log");
        string previous = Environment.GetEnvironmentVariable("NRO_WEB_BUILD_OUTPUT");
        try
        {
            Directory.CreateDirectory(output);
            Environment.SetEnvironmentVariable("NRO_WEB_BUILD_OUTPUT", output);
            WebClientBuild.Run();
            File.WriteAllText(Path.Combine(output, "trigger-result.txt"), "Trigger=Completed\n");
            Debug.Log("ClientWeb combat death trace build completed: " + output);
        }
        catch (Exception ex)
        {
            File.WriteAllText(errorFile, ex.ToString());
            Debug.LogException(ex);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NRO_WEB_BUILD_OUTPUT", previous);
            started = false;
        }
    }
}
