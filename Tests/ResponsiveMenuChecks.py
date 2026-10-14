from pathlib import Path
import subprocess,tempfile
root=Path(__file__).resolve().parents[1]
def method(s,signature,missing=''):
 if signature not in s:return missing
 start=s.index(signature);brace=s.index('{',start);level=1;end=brace+1
 while level:
  if s[end]=='{':level+=1
  elif s[end]=='}':level-=1
  end+=1
 return s[start:end]
stub='''using System;using System.Collections.Generic;
namespace NroWeb {public static class BrowserPlatform {public static bool IsBrowser=true;} }
namespace GAME {
public class Command {public int x,y,w=76,h=24;}
public static class GameCanvas {public static int w,h,hw,hh;public static bool isTouch=true;}
public class MyVector {List<object> items=new List<object>();public void addElement(object o){items.Add(o);}public object elementAt(int i){return items[i];}public int size(){return items.Count;} }
public static class mScreen {public static int cmdW=70,cmdH=24;}
public static class ServerListScreen {public static int ipSelect;}
public class ServerLayout {int mainSelect,w2c,wc,hc,numw,numh;MyVector vecServer=new MyVector();
 SERVER_METHODS
 public void Check(int count){for(int i=0;i<count;i++)vecServer.addElement(new Command());sort();Assert(numw==System.Math.Min(count,System.Math.Max(1,System.Math.Min(3,(GameCanvas.w+5)/81))),"Server grid columns overflow screen or leave empty column");mainSelect=count-1;GameCanvas.w=160;GameCanvas.hw=80;ResizeBrowser();Assert(mainSelect==count-1,"Resize changed selected server");for(int i=0;i<count;i++){var c=(Command)vecServer.elementAt(i);Assert(c.x>=0 && c.x+c.w<=GameCanvas.w,"Server button stayed at old x after rotation");}}
 static void Assert(bool b,string s){if(!b)throw new Exception(s);}
}
public class MenuLayout {Command[] cmd;public static Command cmdDownload,cmdDeleteRMS;
 MENU_METHODS
 public void Check(int count){cmd=new Command[count];for(int i=0;i<count;i++)cmd[i]=new Command{x=1000,y=1000,w=160};cmdDownload=new Command();cmdDeleteRMS=new Command();ResizeBrowser();for(int i=0;i<count;i++){var c=cmd[i];if(c.x<0 || c.x+c.w>GameCanvas.w || c.y<0 || c.y+c.h>GameCanvas.h)throw new Exception("Login menu button clipped after resize");}if(cmdDownload.x<0 || cmdDownload.x+70>GameCanvas.w || cmdDownload.y+24>GameCanvas.h)throw new Exception("Download cancel button clipped");}
}
class Check {static int Main(){try{foreach(var size in new[]{new[]{160,265},new[]{422,177},new[]{512,300}}){GameCanvas.w=size[0];GameCanvas.h=size[1];GameCanvas.hw=size[0]/2;GameCanvas.hh=size[1]/2;foreach(int count in new[]{1,2,4}){GameCanvas.w=size[0];GameCanvas.hw=size[0]/2;new ServerLayout().Check(count);}GameCanvas.w=size[0];GameCanvas.hw=size[0]/2;new MenuLayout().Check(4);new MenuLayout().Check(5);}Console.WriteLine("PASS GAME server grid resize/selection, 4/5-item menu and cancel button bounds");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
} }
'''
for game in ('Game1','Game2'):
 base=root/'Assets/Scripts/Assembly-CSharp'/game
 server=(base/'ServerScr.cs').read_text(encoding='utf8');menu=(base/'ServerListScreen.cs').read_text(encoding='utf8')
 methods=method(server,'private void sort()')+'\n'+method(server,'public void ResizeBrowser()','public void ResizeBrowser(){}')
 layout=method(menu,'public void ResizeBrowser()','public void ResizeBrowser(){}')
 with tempfile.TemporaryDirectory() as d:
  p=Path(d);(p/'Check.cs').write_text(stub.replace('SERVER_METHODS',methods).replace('MENU_METHODS',layout).replace('GAME',game))
  subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/out:'+str(p/'Check.exe'),str(p/'Check.cs')],check=True)
  subprocess.run([str(p/'Check.exe')],check=True)
