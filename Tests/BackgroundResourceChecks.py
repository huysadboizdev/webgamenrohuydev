"""Execute production background resource loading in memory without a Unity build."""
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def method(source, signature):
    start = source.index(signature)
    brace = source.index('{', start)
    depth, end = 1, brace + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


FIXTURE = r'''
using System;
using System.Collections.Generic;
namespace NroWeb { public static class BrowserPlatform { public static bool IsBrowser; } }
namespace GAME {
public class Texture2D { public int width,height; public Texture2D(int w,int h){width=w;height=h;} }
public class Image {
 public Texture2D texture; public int w,h;
 public Image(){texture=new Texture2D(1,1);}
 public Image(Texture2D t){texture=t;w=t.width;h=t.height;}
 public static Dictionary<string,Texture2D> assets=new Dictionary<string,Texture2D>();
 public static Image createImage(string path){if(!assets.ContainsKey(path))throw new Exception("missing");return new Image(assets[path]);}
 public static Image createImage(sbyte[] bytes,int offset,int length){return new Image(new Texture2D(bytes[0],bytes[1]));}
 public static void setTextureQuality(Texture2D texture){}
}
public static class Main { public static string res="res"; }
public static class mGraphics { public static int zoomLevel; }
public static class Res { public static string[] split(string s,string separator,int unused){return s.Split(new[]{separator},StringSplitOptions.None);} }
public static class Rms { public static sbyte[] bytes; public static sbyte[] loadRMS(string key){return bytes;} }
public static class Cout { public static void LogError(string s){} }
public static class TileMap { public static int lastBgID=-1,lastType=-1,bgType,pxh; }
public static class BackgroudEffect { public static int yfog; public static void clearImage(){} }
public class GameCanvas {
 public static int typeBg,transY; public static bool isLoadBGok; public static int attempts;
 static void randomRaintEff(int unused){}
 public static Texture2D Resize(Texture2D source,int w,int h){return new Texture2D(w,h);}
 LOAD
 CUT
 CACHE
}
public static class Check {
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){
  Texture2D packaged=new Texture2D(32,17); Image.assets["res/x1/bg/b10"]=packaged;
  foreach(int zoom in new[]{2,3,4}){
   mGraphics.zoomLevel=zoom; NroWeb.BrowserPlatform.IsBrowser=true; Rms.bytes=null;
   Image image=GameCanvas.loadImageRMS("/bg/b10.png");
   Assert(image!=null,"Browser background b10 missing at zoom "+zoom);
   Assert(image.w==32*zoom && image.h==17*zoom,"Fallback dimensions wrong at zoom "+zoom);
   Assert(!Object.ReferenceEquals(image.texture,packaged),"Fallback mutated shared texture");
   Assert(packaged.width==32 && packaged.height==17,"Packaged texture modified");
   NroWeb.BrowserPlatform.IsBrowser=false;
   Assert(GameCanvas.loadImageRMS("/bg/b10.png")==null,"Native missing-resource behavior changed");
   NroWeb.BrowserPlatform.IsBrowser=true;
   Rms.bytes=new sbyte[]{11,9}; image=GameCanvas.loadImageRMS("/bg/b10.png");
   Assert(image.w==11 && image.h==9,"RMS resource lost priority");
   Texture2D current=new Texture2D(80,50); string path="res/x"+zoom+"/bg/b10";Image.assets[path]=current;
   image=GameCanvas.loadImageRMS("/bg/b10.png");Assert(Object.ReferenceEquals(image.texture,current),"Current zoom resource lost priority");
   Image.assets.Remove(path);Rms.bytes=null;
   Image.assets["res/x1/item/example"]=packaged;
   Assert(GameCanvas.loadImageRMS("/item/example.png")==null,"Non-background fallback introduced");
  }
  mGraphics.zoomLevel=1;Image one=GameCanvas.loadImageRMS("/bg/b10.png");
  Assert(Object.ReferenceEquals(one.texture,packaged),"Existing x1 texture copied unnecessarily");
  TileMap.lastBgID=-1;TileMap.lastType=-1;GameCanvas.attempts=0;
  GameCanvas.loadBG(1);GameCanvas.loadBG(1);
  Assert(GameCanvas.attempts==2,"Failed background cached: same background did not retry");
  Assert(!GameCanvas.isLoadBGok,"Failed load reported success");
  Console.WriteLine("PASS GAME background fallback x2/x3/x4; texture ownership, native/current/RMS priority, non-background behavior, failed-load retry");
 }
}
}
'''

failures = []
for game in ('Game1', 'Game2'):
    source = (ROOT / 'Assets/Scripts/Assembly-CSharp' / game / 'GameCanvas.cs').read_text(encoding='utf8')
    loading = method(source, 'public static void loadBG(int typeBG)')
    # Execute the actual cache guard/commit and catch, injecting an exception at
    # the asset-loading boundary to reproduce a failed request without game state.
    prefix = loading[:loading.index('\t\t\t\tlayerSpeed =')]
    catch = loading[loading.rindex('\t\t\tcatch (Exception)'):]
    cache = prefix + 'attempts++; throw new Exception("asset load failure");\n}\n' + catch
    fixture = FIXTURE.replace('GAME', game).replace('LOAD', method(source, 'public static Image loadImageRMS(string path)'))
    fixture = fixture.replace('CUT', method(source, 'public static string cutPng(string str)')).replace('CACHE', cache)
    # Windows PowerShell's compiler predates C# index-from-end syntax.
    fixture = fixture.replace('array[^1]', 'array[array.Length - 1]')
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / 'BackgroundChecks.cs'
        path.write_text(fixture, encoding='utf8')
        result = subprocess.run(['powershell', '-NoProfile', '-NonInteractive', '-Command',
            "$ErrorActionPreference='Stop'; Add-Type -Path '" + str(path).replace("'", "''") + "'; [" + game + '.Check]::Run()'], text=True, capture_output=True)
        print(result.stdout.strip())
        if result.returncode:
            print(result.stderr.strip())
            failures.append(game)
if failures:
    raise SystemExit('FAIL background resources: ' + ', '.join(failures))
