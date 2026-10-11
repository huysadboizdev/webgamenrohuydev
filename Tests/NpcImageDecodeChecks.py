from pathlib import Path
import subprocess,tempfile,sys
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]
for game in ('Game1','Game2'):
 s=(root/f'Assets/Scripts/Assembly-CSharp/{game}/Image.cs').read_text(encoding='utf-8-sig')
 start=s.index('private static Image __createImage(byte[] imageData)')
 end=s.index('private static Image __createImage(Image src',start)
 method=s[start:end]
 shell="""using System;
namespace UnityEngine { public class Object {public static void Destroy(object o){}} }
public class Texture {public int width=1,height=1;public bool LoadImage(byte[] b){if(b[0]==2)throw new Exception(); if(b[0]==3){width=16;height=24;return true;}return false;} }
public class Cout {public static void LogError(string s){} }
public class Image {public Texture texture=new Texture(); public int w,h;static void setTextureQuality(Image i){} METHOD
static void Main(){
if(__createImage(new byte[]{1})!=null)throw new Exception("Failed decode returned a blank image");
if(__createImage(new byte[]{2})!=null)throw new Exception("Decode exception returned a blank image");
var good=__createImage(new byte[]{3});if(good==null||good.w!=16||good.h!=24)throw new Exception("Valid decode failed");
if(__createImage(null)!=null||__createImage(new byte[0])!=null)throw new Exception("Empty decode failed");
Console.WriteLine("PASS actual decoder: failed result, exception, valid image, empty data");} }
""".replace('METHOD',method)
 with tempfile.TemporaryDirectory() as d:
  cs=Path(d)/'test.cs';exe=Path(d)/'test.exe';cs.write_text(shell)
  subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/out:'+str(exe),str(cs)],check=True)
  subprocess.run([str(exe)],check=True)
