from pathlib import Path
import subprocess,tempfile
root=Path(__file__).resolve().parents[1]
stub='''using System;
namespace NroWeb { public static class BrowserPlatform { public static bool IsBrowser=true; } }
namespace GAME {
public delegate void ActionChat(string s);
public interface IActionListener { void perform(int id,object p); }
public interface IChatable { void onChatFromMe(string s,string to); void onCancelChat(); }
public class Command { public string caption;public int x,y; public ActionChat actionChat;public Action action; IActionListener listener;int id;
 public Command(){} public Command(string s,IActionListener a,int i,object p,int x,int y){caption=s;listener=a;id=i;this.x=x;this.y=y;}
 public void perform(string s){if(actionChat!=null)actionChat(s);} public void performAction(){if(listener!=null)listener.perform(id,null);if(action!=null)action();} }
public class TField { public bool showSubTextField,isPaintMouse,isFocus,justReturnFromTextBox;public string name,strInfo;public int width,height,x,y;public Command cmdDoneAction;string text="";
 public void setMaxTextLenght(int n){} public void keyPressed(int n){}public string getText(){return text;}public void setText(string s){text=s;}public void setFocusWithKb(bool isFocus){this.isFocus=isFocus;}public void clearAll(){} public void clearKb(){}public void clear(){text="";}public void update(){}public void paint(mGraphics g){} }
public static class Main { public static bool isWindowsPhone,isIPhone,isPC; }
public static class mScreen { public static int ITEM_HEIGHT=20,cmdH=20; }
public static class GameCanvas { public static int w=844,h=350;public static object currentDialog;public static bool isTouch=true;public static bool[] keyPressed=new bool[30];public static Paint paintz=new Paint(); }
public class Paint { public void paintCmdBar(mGraphics g,Command a,Command b,Command c){} }
public static class mResources { public static string OK="OK",DELETE="Delete",CLOSE="Close"; }
public static class ipKeyboard { public static int TEXT;public static void openKeyBoard(string s,int t,string text,Command c){throw new Exception("Native keyboard called on WebGL");} }
public class mGraphics{} public static class PopUp { public static void paintPopUp(mGraphics g,int a,int b,int c,int d,int e,bool isButton){} }
public class mFont { public static mFont tahoma_7b_green2=new mFont();public void drawString(mGraphics g,string s,int a,int b,int c){} }
public static class Cout { public static void LogError(string s){} }
public static class mSystem { public static long currentTimeMillis(){return 5000;} }
class Check : IChatable { public int calls; public string last;public void onChatFromMe(string s,string to){calls++;last=s;}public void onCancelChat(){}
static void Assert(bool b,string s){if(!b)throw new Exception(s);}
static int Main(){try{var c=new ChatTextField();c.initChatTextField();var receiver=new Check();
for(int i=0;i<2;i++){c.startChat2(receiver,"");c.tfChat.setText(i==0?"12":"3");c.tfChat.cmdDoneAction.performAction();}
Assert(receiver.calls==2 && receiver.last=="3","Second mobile setup answer was dropped within one second");
Assert(!c.isShow,"Setup chat did not close after Done");
c.parentScreen=receiver;c.startChat(receiver,"");c.tfChat.setText("hello");c.tfChat.cmdDoneAction.performAction();Assert(receiver.calls==3 && receiver.last=="hello","Normal chat retained setup callback");
Console.WriteLine("PASS GAME mobile normal chat and two consecutive setup answers without native keyboard");return 0;
}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
} }
'''
for game in ('Game1','Game2'):
 with tempfile.TemporaryDirectory() as d:
  p=Path(d);(p/'Stubs.cs').write_text(stub.replace('GAME',game))
  subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/out:'+str(p/'Check.exe'),str(p/'Stubs.cs'),str(root/'Assets/Scripts/Assembly-CSharp'/game/'ChatTextField.cs')],check=True)
  subprocess.run([str(p/'Check.exe')],check=True)
