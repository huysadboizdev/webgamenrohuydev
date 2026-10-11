from pathlib import Path
import subprocess,tempfile
root=Path(__file__).resolve().parents[1]
stub='''using System;
namespace GAME {
public class Image{}
public class DataInputStream{public myReader r;}
public static class MyStream{public static DataInputStream readFile(string s){return null;}}
public static class Res{public static void outz(string s){} public static int abs(int n){return System.Math.Abs(n);}}
public class mGraphics{public void drawRegion(Image i,int a,int b,int c,int d,int e,int f,int g,int h){}}
public static class StaticObj{public static int TOP_RIGHT,VCENTER_HCENTER;}
public static class GameCanvas{public static int transY;}
class Check{
static sbyte[] Bytes(string p){var b=System.IO.File.ReadAllBytes(p);var s=new sbyte[b.Length];Buffer.BlockCopy(b,0,s,0,b.Length);return s;}
static void Assert(bool b,string s){if(!b)throw new Exception(s);}
static int Main(string[] args){try{
foreach(int id in new[]{62,63,65,70}){var e=new EffectData{ID=id};e.readData(Bytes(args[0]+"/DataEffect_"+id));Assert(e.arrFrame!=null && e.arrFrame.Length>0,"16-bit effect failed: "+id);Assert(e.frame!=null && e.frame.Length>0,"Missing frames");}
int valid=0;
var coordinates=new EffectData{ID=62};coordinates.readData(Bytes(args[0]+"/DataEffect_62"));Assert(coordinates.imgInfo[0].x0==191 && coordinates.imgInfo[0].y0==65 && coordinates.imgInfo[0].w==12 && coordinates.imgInfo[0].h==31,"Wrong decoded 16-bit coordinates");
var animated=new EffectData{ID=201};animated.readData(Bytes(args[0]+"/DataEffect_201"));Assert(animated.get(1).Length==100 && animated.get(2).Length==16 && animated.get(2)[0]==2 && animated.get(10).Length==16,"Animation markers changed");
foreach(var p in System.IO.Directory.GetFiles(args[0],"DataEffect_*")){int id=int.Parse(System.IO.Path.GetFileName(p).Substring(11));if(id>=281&&id<=284)continue;var e=new EffectData{ID=id};if(id==60)e.readDataNewBoss(Bytes(p),2);else e.readData(Bytes(p));Assert(e.arrFrame!=null,"Server effect failed "+id);valid++;}
var saved=new EffectData{ID=62};var input=Bytes(args[0]+"/DataEffect_62");saved.readData(input);var before=saved.frame;
for(int n=0;n<input.Length-2;n++){var broken=new sbyte[n];Array.Copy(input,broken,n);saved.readData(broken);Assert(Object.ReferenceEquals(saved.frame,before),"Truncated effect partially replaced valid state at "+n);}
saved.readData(new sbyte[]{1,0,0,0,1,1,0,1,1,0,0,0,0,100,0,1,0,0});Assert(Object.ReferenceEquals(saved.frame,before),"Invalid sprite replaced valid state");
Assert(saved.get(-1)!=null,"Negative animation index");saved.paintFrame(new mGraphics(),-1,0,0,0,0);saved.paintFrame(new mGraphics(),9999,0,0,0,0);
saved.img=new Image();saved.paintFrame(new mGraphics(),-1,0,0,0,0);saved.paintFrame(new mGraphics(),9999,0,0,0,0);
Console.WriteLine("PASS GAME "+valid+" real server effects, short-coordinate fallback, truncated/invalid data, atomic state and render bounds");return 0;
}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}}
'''
for game in ('Game1','Game2'):
    base=root/'Assets/Scripts/Assembly-CSharp'/game
    with tempfile.TemporaryDirectory() as d:
        p=Path(d);(p/'Stubs.cs').write_text(stub.replace('GAME',game))
        sources=[str(base/(name+'.cs')) for name in ('EffectData','Frame','ImageInfo','myReader')]
        subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/out:'+str(p/'Check.exe'),str(p/'Stubs.cs'),*sources],check=True)
        subprocess.run([str(p/'Check.exe'),str(root/'Tests/Fixtures/Effects')],check=True)
