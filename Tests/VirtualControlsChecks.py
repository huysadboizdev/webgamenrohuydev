from pathlib import Path
import re,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
def method(source,name):
 start=source.index('public void '+name+'()');brace=source.index('{',start);level=1;end=brace+1
 while level:
  if source[end]=='{':level+=1
  elif source[end]=='}':level-=1
  end+=1
 return source[start:end]
stub='''using System;
namespace UnityEngine {public enum KeyCode { UpArrow,DownArrow,LeftArrow,RightArrow } public static class Input {public static bool right;public static bool GetKey(KeyCode k){return k==KeyCode.RightArrow && right;}} }
namespace NroWeb { public static class BrowserPlatform { public static bool IsBrowser; } }
namespace GAME {
public static class Main { public static bool isPC=true,isIPhone; }
public static class Rms { public static int saved=-1;public static int loadRMSInt(string key){return saved;} public static void saveRMSInt(string key,int v){saved=v;} }
public class Image{}
public class mGraphics {public const int HCENTER=1,VCENTER=2; public int draws;public void drawImage(Image i,int x,int y,int anchor){if(i==null || x<=0 || y<=0)throw new Exception("Invalid joystick paint");draws++;}}
public class GameScr {public static GamePad gamePad;public static int isAnalog,layoutChanges;public static Image imgAnalog1=new Image(),imgAnalog2=new Image();public static void setSkillBarPosition(){layoutChanges++;}
 public static void Restore(){RESTORE}
 public static void TouchRelease(){TOUCH_RELEASE}
}
public class SoundMn {public void getStrOption(){}public void chatVipToolOption(){throw new Exception("Wrong configuration action");} METHODS }
public class Point {public int x,y;}
public static class GameCanvas {public static int w=512,h=300,hw=256,hh=150,pxFirst,pyFirst,px,py;public static bool isTouch=true,isPointerDown,isPointerJustRelease;public static bool[] keyHold=new bool[30],keyPressed=new bool[30];public static Point[] arrPos={new Point(),new Point(),new Point()};
 public static void clearKeyHold(){Array.Clear(keyHold,0,keyHold.Length);}public static bool isPointerHoldIn(int x,int y,int w,int h){return px>=x&&py>=y&&px<x+w&&py<y+h;} }
public class Char {public int statusMe;static Char c=new Char();public static Char myCharz(){return c;}}
public static class Math {public static int pow(int a,int b){return (int)System.Math.Pow(a,b);}public static int abs(int a){return System.Math.Abs(a);}}
public static class Res {public static int sqrt(int n){return (int)System.Math.Sqrt(n);}public static int abs(int n){return System.Math.Abs(n);}public static int angle(int x,int y){return ((int)(System.Math.Atan2(y,x)*180/System.Math.PI)+360)%360;}public static bool inRect(int x,int y,int w,int h,int px,int py){return px>=x&&py>=y&&px<x+w&&py<y+h;}}
class Check {static void Assert(bool b,string s){if(!b)throw new Exception(s);} static void Defaults(bool pc,bool mobile,bool web,int saved,int expected){GAME.Main.isPC=pc;GAME.Main.isIPhone=mobile;NroWeb.BrowserPlatform.IsBrowser=web;Rms.saved=saved;GameScr.Restore();Assert(GameScr.isAnalog==expected,"Wrong restored joystick choice");}
static int Main(){try{
 GAME.Main.isPC=true;GAME.Main.isIPhone=false;NroWeb.BrowserPlatform.IsBrowser=true;GameScr.isAnalog=0;var option=new SoundMn();option.CaseAnalog();Assert(GameScr.isAnalog==0,"PC configuration must preserve native checkbox behavior");
 GAME.Main.isPC=false;GAME.Main.isIPhone=true;option.CaseAnalog();Assert(GameScr.isAnalog==1 && Rms.saved==1,"Mobile checkbox cannot enable controls");option.CaseAnalog();Assert(GameScr.isAnalog==0 && Rms.saved==0,"Mobile checkbox cannot disable controls");
 Defaults(true,false,true,1,0);Defaults(true,false,true,0,0);Defaults(true,false,true,-1,0);Defaults(false,true,true,-1,1);Defaults(false,true,true,0,0);Defaults(false,true,true,1,1);Defaults(true,false,false,1,0);
 foreach(bool pc in new[]{true,false}){GAME.Main.isPC=pc;GameScr.isAnalog=1;GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=false;var pad=new GamePad();pad.update();var g=new mGraphics();pad.paint(g);Assert(g.draws==2,"Joystick not painted");
 foreach(var v in new[]{new[]{30,0,pc?24:6},new[]{-30,0,pc?23:4},new[]{0,-30,pc?21:2},new[]{0,30,pc?22:8}}){GameCanvas.pxFirst=80;GameCanvas.pyFirst=200;GameCanvas.px=80+v[0];GameCanvas.py=200+v[1];GameCanvas.isPointerDown=true;pad.update();Assert(GameCanvas.keyHold[v[2]],"Virtual direction not mapped");GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=true;pad.update();Assert(!GameCanvas.keyHold[v[2]],"Direction stuck after release");GameCanvas.isPointerJustRelease=false;}}
 foreach(var size in new[]{new[]{195,404},new[]{422,177},new[]{320,240}}){GAME.Main.isPC=false;GameCanvas.clearKeyHold();GameCanvas.w=size[0];GameCanvas.h=size[1];GameCanvas.hw=size[0]/2;GameCanvas.hh=size[1]/2;GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=false;var p=new GamePad();p.update();p.paint(new mGraphics());GameCanvas.pxFirst=45;GameCanvas.pyFirst=size[1]-(p.isLargeGamePad?45:90);GameCanvas.px=75;GameCanvas.py=GameCanvas.pyFirst;GameCanvas.isPointerDown=true;p.update();Assert(GameCanvas.keyHold[6],"Mobile portrait/landscape touch zone failed");GameCanvas.isPointerDown=false;GameCanvas.isPointerJustRelease=true;p.update();Assert(!GameCanvas.keyHold[6],"Mobile direction stuck after release");GameCanvas.isPointerJustRelease=false;}
 Console.WriteLine("PASS GAME native PC checkbox, mobile persisted choice/defaults, joystick paint, both direction maps and pointer release");return 0;
}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
} }
'''
for game in ('Game1','Game2'):
 base=root/'Assets/Scripts/Assembly-CSharp'/game
 source=(base/'SoundMn.cs').read_text(encoding='utf8')
 methods='\n'.join(method(source,name) for name in ('analogToolOption','CaseAnalog'))
 restore=re.search(r'^\s*isAnalog = .*loadRMSInt\("analog"\).*;$',(base/'GameScr.cs').read_text(encoding='utf8'),re.M).group().strip()
 scr=(base/'GameScr.cs').read_text(encoding='utf8');start=scr.index('private void UpdateKeyTouchControl()');end=scr.index('public void setCharJump',start);part=scr[start:end]
 start=part.rindex('if (GameCanvas.isPointerJustRelease)');brace=part.index('{',start);level=1;end=brace+1
 while level:
  if part[end]=='{':level+=1
  elif part[end]=='}':level-=1
  end+=1
 release=part[start:end]
 with tempfile.TemporaryDirectory() as d:
  p=Path(d);(p/'Check.cs').write_text(stub.replace('METHODS',methods).replace('RESTORE',restore).replace('TOUCH_RELEASE',release).replace('GAME',game))
  subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/out:'+str(p/'Check.exe'),str(p/'Check.cs'),str(base/'GamePad.cs')],check=True)
  subprocess.run([str(p/'Check.exe')],check=True)
