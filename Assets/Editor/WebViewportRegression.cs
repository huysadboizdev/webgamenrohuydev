using System;
using System.Reflection;
using UnityEngine;

public static class WebViewportRegression
{
    public static void Run()
    {
        foreach (string game in new[] {"Game1", "Game2"}) {
            Assembly assembly = typeof(Management).Assembly;
            Type canvas = assembly.GetType(game + ".GameCanvas");
            Type screen = assembly.GetType(game + ".GameScr");
            MethodInfo refresh = screen.GetMethod("RefreshBrowserControls", BindingFlags.Public | BindingFlags.Static);
            if (refresh == null) throw new Exception("Browser resize does not refresh movement pad: " + game);
            int oldW=(int)canvas.GetField("w").GetValue(null), oldH=(int)canvas.GetField("h").GetValue(null);
            int oldHw=(int)canvas.GetField("hw").GetValue(null), oldHh=(int)canvas.GetField("hh").GetValue(null);
            object oldPad=screen.GetField("gamePad").GetValue(null);
            bool oldSmall=(bool)canvas.GetField("isTouchControlSmallScreen").GetValue(null);
            bool oldLarge=(bool)canvas.GetField("isTouchControlLargeScreen").GetValue(null);
            try {
                foreach(int width in new[]{220,600,320}) {
                    int height=width==220?400:200;
                    canvas.GetField("w").SetValue(null,width);canvas.GetField("h").SetValue(null,height);
                    canvas.GetField("hw").SetValue(null,width/2);canvas.GetField("hh").SetValue(null,height/2);
                    refresh.Invoke(null,null);
                    object pad=screen.GetField("gamePad").GetValue(null);Type type=pad.GetType();
                    if((bool)type.GetField("isLargeGamePad").GetValue(pad)!=(width>380))throw new Exception("Pad mode stale after rotation: "+game);
                    int expectedWidth=width>380?(width/2)/4*3-20:width/2;
                    if((int)type.GetField("wZone").GetValue(pad)!=expectedWidth)throw new Exception("Pad hit width stale: "+game);
                    if((int)type.GetField("hZone").GetValue(pad)!=(width>380?height:height-80))throw new Exception("Pad hit height stale: "+game);
                    if((bool)canvas.GetField("isTouchControlSmallScreen").GetValue(null)!=(width<320))throw new Exception("Touch size stale: "+game);
                }
            } finally {
                canvas.GetField("w").SetValue(null,oldW);canvas.GetField("h").SetValue(null,oldH);
                canvas.GetField("hw").SetValue(null,oldHw);canvas.GetField("hh").SetValue(null,oldHh);
                canvas.GetField("isTouchControlSmallScreen").SetValue(null,oldSmall);
                canvas.GetField("isTouchControlLargeScreen").SetValue(null,oldLarge);
                screen.GetField("gamePad").SetValue(null,oldPad);
            }
        }
        Debug.Log("WebViewportRegression PASS (both tabs movement bounds and touch sizing after rotation)");
    }
}
