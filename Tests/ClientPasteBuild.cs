using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ClientPasteBuild
{
    public static void Run()
    {
        ClipboardTextTests.Run();
        foreach (Type type in new[] { typeof(Game1.TField), typeof(Game2.TField) })
        {
            object field = FormatterServices.GetUninitializedObject(type);
            Set(field, "text", "ac"); Set(field, "caretPos", 1);
            Set(field, "maxTextLenght", 80); Set(field, "inputType", 0);
            type.GetMethod("pasteText").Invoke(field, new object[] { "b\r\nNguyệt" });
            Equal(type.GetMethod("getText").Invoke(field, null), "ab Nguyệtc");
            Equal(Get(field, "caretPos"), 9);
            Set(field, "inputType", 1); Set(field, "text", "12"); Set(field, "caretPos", 1);
            type.GetMethod("pasteText").Invoke(field, new object[] { "x3" });
            Equal(type.GetMethod("getText").Invoke(field, null), "132");
            Set(field, "inputType", 2);
            type.GetMethod("pasteText").Invoke(field, new object[] { "!" });
            Equal(Get(field, "paintedText"), "****");
        }
        Debug.Log("PASS both actual TField implementations: caret, newline, numeric, password masking");
        string oldClipboard = GUIUtility.systemCopyBuffer;
        try
        {
            GUIUtility.systemCopyBuffer = "i 556 1 35:1 103:7";
            CheckRoutes("Game1");
            CheckRoutes("Game2");
        }
        finally { GUIUtility.systemCopyBuffer = oldClipboard; }
        Debug.Log("PASS both actual canvas routes: chat, input dialog, modal blocking, login focus");
        string folder = Environment.GetEnvironmentVariable("CLIENT_PASTE_OUTPUT");
        if (string.IsNullOrEmpty(folder)) throw new Exception("CLIENT_PASTE_OUTPUT is required");
        Directory.CreateDirectory(folder);
        // The isolated build uses Mono; the user's Editor/player settings remain untouched.
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(folder, "NRO-Paste.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        var summary = report.summary;
        File.WriteAllText(Path.Combine(folder, "build-result.txt"),
            "Result=" + summary.result + "\nErrors=" + summary.totalErrors + "\nWarnings=" + summary.totalWarnings
            + "\nTests=16 pure insertion cases + both actual TField implementations\n");
        if (summary.result != BuildResult.Succeeded) throw new Exception("Client build failed: " + summary.result);
        Debug.Log("CLIENT_PASTE_BUILD_SUCCEEDED " + folder);
    }
    private static void Set(object target, string name, object value) { target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value); }
    private static object Get(object target, string name) { return target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target); }
    private static void Equal(object actual, object expected) { if (!object.Equals(actual, expected)) throw new Exception("Expected " + expected + "; got " + actual); }
    private static void CheckRoutes(string ns)
    {
        var assembly = typeof(Game1.TField).Assembly;
        Func<string, Type> t = name => assembly.GetType(ns + "." + name, true);
        Func<string, object> create = name => FormatterServices.GetUninitializedObject(t(name));
        Func<object> field = () => {
            var f = create("TField"); Set(f, "text", ""); Set(f, "maxTextLenght", 80);
            t("TField").GetField("isFocus").SetValue(f, true); return f;
        };
        var canvas = create("GameCanvas");
        var chat = create("ChatTextField");
        var chatField = field();
        t("ChatTextField").GetField("tfChat").SetValue(chat, chatField);
        t("ChatTextField").GetField("isShow").SetValue(chat, true);
        t("ChatTextField").GetField("right").SetValue(chat, Activator.CreateInstance(t("Command")));
        t("ChatTextField").GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, chat);
        t("GameCanvas").GetField("menu").SetValue(null, null);
        t("GameCanvas").GetField("currentDialog").SetValue(null, null);
        Action paste = () => t("GameCanvas").GetMethod("pasteClipboard").Invoke(canvas, null);
        paste(); Equal(Get(chatField, "text"), "i 556 1 35:1 103:7");
        var dialog = create("InputDlg"); var dialogField = field();
        t("InputDlg").GetField("tfInput").SetValue(dialog, dialogField);
        t("GameCanvas").GetField("currentDialog").SetValue(null, dialog);
        paste(); Equal(Get(dialogField, "text"), "i 556 1 35:1 103:7");
        Equal(Get(chatField, "text"), "i 556 1 35:1 103:7");
        t("GameCanvas").GetField("currentDialog").SetValue(null, create("MsgDlg"));
        paste(); Equal(Get(chatField, "text"), "i 556 1 35:1 103:7");
        var login = create("LoginScr"); var user = field(); var pass = field();
        t("TField").GetField("isFocus").SetValue(pass, false);
        t("LoginScr").GetField("tfUser").SetValue(login, user);
        t("LoginScr").GetField("tfPass").SetValue(login, pass);
        t("ChatTextField").GetField("isShow").SetValue(chat, false);
        t("GameCanvas").GetField("currentDialog").SetValue(null, null);
        t("GameCanvas").GetField("currentScreen").SetValue(null, login);
        paste(); Equal(Get(user, "text"), "i 556 1 35:1 103:7"); Equal(Get(pass, "text"), "");
    }
}
