"""Compile the actual SmallImage class, with a controlled network and clock."""
from pathlib import Path
import subprocess, tempfile, sys
root=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parents[1]
csc='C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe'
for game in ('Game1','Game2'):
 source=(root/f'Assets/Scripts/Assembly-CSharp/{game}/SmallImage.cs').read_text(encoding='utf-8-sig')
 shell='''using System; using System.Collections.Generic;
namespace NS.Assets.src.e { public class Small {public Image img; public Small(Image i,int id){img=i;} public void paint(mGraphics g,int t,int x,int y,int a){} public void paint(mGraphics g,int t,int f,int x,int y,int w,int h,int a){} public void update(){} } }
namespace NS {
 public class Image {public int width=8,height=8; public static Image createRGBImage(int[] a,int w,int h,bool bl){return new Image{width=w,height=h};}}
 public class MyVector {}
 public class DataInputStream { public DataInputStream(sbyte[] b){} public short readShort(){return 0;} public int readUnsignedByte(){return 0;} }
 public class Rms {public static sbyte[] loadRMS(string k){return null;}}
 public class Cout { public static void LogError3(string s){} }
 public class mGraphics {public static int zoomLevel=2;public static int getImageWidth(Image i){return i.width;}public static int getImageHeight(Image i){return i.height;} public void drawRegion(object i,params object[] a){} }
 public class GameCanvas {public static int gameTick=1;public static bool lowGraphic;public static object currentScreen;public static Image local;public static Image loadImage(string p){return local;}public static Image loadImageRMS(string p){return local;}}
 public class GameScr {public static GameScr instance=new GameScr();public static GameScr gI(){return instance;}}
 public class mSystem {public static long now;public static long currentTimeMillis(){return now;}}
 public class Session_ME {public static bool online=true;public static Session_ME instance=new Session_ME();public static Session_ME gI(){return instance;}public bool isConnected(){return online;}}
 public class Session_ME2 {public static Session_ME2 instance=new Session_ME2();public static Session_ME2 gI(){return instance;}public bool isConnected(){return false;}}
 public class Service {public static int requests;public static Service instance=new Service();public static Service gI(){return instance;}public void requestIcon(int id){requests++;}}
 public class Tests {static void Check(bool value,string reason){if(!value)throw new Exception(reason);} static void Main(){
  SmallImage.imgEmpty=Image.createRGBImage(new int[1],1,1,true);
  SmallImage.createImage(4521);Check(Service.requests==1,"Initial request");
  mSystem.now=2999;SmallImage.update();Check(Service.requests==1,"Do not spam before retry interval");
  mSystem.now=3000;SmallImage.update();Check(Service.requests==2,"Dropped NPC image response must retry without clear data");
  SmallImage.ReceiveImage(4521,null);Check(!SmallImage.imageRaw.ContainsKey(4521),"Failed image must not poison raw cache");
  mSystem.now=8999;SmallImage.update();Check(Service.requests==2,"Backoff before second retry");
  mSystem.now=9000;SmallImage.update();Check(Service.requests==3,"Invalid response must recover");
  var valid=new Image();SmallImage.ReceiveImage(4521,valid);mSystem.now=60000;SmallImage.update();Check(Service.requests==3,"Success cancels retries");
  SmallImage.ReceiveImage(4521,null);Check(SmallImage.imgNew[4521].img==valid,"Late failure cannot erase good image");
  SmallImage.createImage(4542);Session_ME.online=false;mSystem.now=90000;SmallImage.update();Check(Service.requests==4,"Offline retries must pause");
  Session_ME.online=true;SmallImage.update();Check(Service.requests==5,"Reconnect resumes missing sprite");SmallImage.ReceiveImage(4542,new Image());
  GameCanvas.local=new Image();SmallImage.createImage(4541);mSystem.now=120000;SmallImage.update();Check(Service.requests==5,"Local images never request network");GameCanvas.local=null;
  for(int id=4600;id<4620;id++)SmallImage.createImage(id);int before=Service.requests;mSystem.now+=3000;SmallImage.update();Check(Service.requests-before<=4,"Bound per-tick retries");
  Console.WriteLine("PASS dropped/corrupt NPC sprites, backoff, good cache, offline/reconnect, local sprites and bounded retries");
 } }
}'''.replace('NS',game)
 with tempfile.TemporaryDirectory(prefix='npc-image-tests-') as temp:
  p=Path(temp);(p/'Source.cs').write_text(source,encoding='utf8');(p/'Shell.cs').write_text(shell,encoding='utf8')
  subprocess.run([csc,'/nologo','/out:'+str(p/'Checks.exe'),str(p/'Source.cs'),str(p/'Shell.cs')],check=True)
  subprocess.run([str(p/'Checks.exe')],check=True)
 print(game+' actual SmallImage passed')
