from pathlib import Path
import subprocess,tempfile,json
root=Path(__file__).resolve().parents[1]
stub='''using System;
public enum TabType { Tab1,Tab2 } public static class Management { public static TabType tab; }
namespace UnityEngine { public enum TouchScreenKeyboardType { ASCIICapable,NumberPad } public class TouchScreenKeyboard { public static bool hideInput; public bool done; public string text; public static TouchScreenKeyboard Open(string text,TouchScreenKeyboardType type,bool autocorrection,bool multiline,bool secure,bool alert,string caption) { throw new Exception("Native keyboard reached"); } } }
namespace NroWeb { public static class BrowserPlatform { public static bool IsBrowser=true,HasCommandInput;public static string InputValue;public static int InputState;public static void OpenInput(string text,bool password,bool numeric,bool field=true){InputValue=text;InputState=1;HasCommandInput=!field;}public static void CloseInput(){InputState=0;HasCommandInput=false;} } }
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

public class mGraphics{} public static class PopUp { public static void paintPopUp(mGraphics g,int a,int b,int c,int d,int e,bool isButton){} }
public class mFont { public static mFont tahoma_7b_green2=new mFont();public void drawString(mGraphics g,string s,int a,int b,int c){} }
public static class Cout { public static void LogError(string s){} }
public static class mSystem { public static long currentTimeMillis(){return 5000;} }
class Check : IChatable { public int calls; public string last;public void onChatFromMe(string s,string to){calls++;last=s;}public void onCancelChat(){}
static void Assert(bool b,string s){if(!b)throw new Exception(s);}
static int Main(){try{var c=new ChatTextField();c.initChatTextField();var receiver=new Check();
Management.tab=ACTIVE;
for(int i=0;i<2;i++){c.startChat2(receiver,"");Assert(NroWeb.BrowserPlatform.HasCommandInput,"Chat must open browser command input");NroWeb.BrowserPlatform.InputValue=i==0?"12":"3";NroWeb.BrowserPlatform.InputState=2;ipKeyboard.update();ipKeyboard.update();}
Assert(receiver.calls==2 && receiver.last=="3","Second mobile setup answer was dropped within one second");
Assert(!c.isShow,"Setup chat did not close after Done");
c.parentScreen=receiver;c.startChat(receiver,"");NroWeb.BrowserPlatform.InputValue="hello";NroWeb.BrowserPlatform.InputState=2;ipKeyboard.update();ipKeyboard.update();Assert(receiver.calls==3 && receiver.last=="hello","Normal chat retained setup callback");
c.startChat2(receiver,"");ipKeyboard.CancelBrowser();NroWeb.BrowserPlatform.InputValue="cancelled";NroWeb.BrowserPlatform.InputState=2;ipKeyboard.update();Assert(receiver.calls==3,"Cancelled command dispatched callback");NroWeb.BrowserPlatform.CloseInput();
c.startChat2(receiver,"");Management.tab=INACTIVE;NroWeb.BrowserPlatform.InputValue="inactive";NroWeb.BrowserPlatform.InputState=2;ipKeyboard.update();Assert(receiver.calls==3,"Inactive tab consumed input");Management.tab=ACTIVE;ipKeyboard.update();Assert(receiver.calls==4 && receiver.last=="inactive","Active tab did not consume pending input");
Console.WriteLine("PASS GAME browser command delegation, consecutive setup answers, normal chat, exactly-once callbacks, cancellation and active-tab ownership");return 0;
}catch(Exception e){Console.Error.WriteLine(e);return 1;}}
} }
'''
# Use Unity's Roslyn compiler: ipKeyboard uses current C# named-argument syntax.
data=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data')
dotnet=data/'NetCoreRuntime/dotnet.exe'
for game in ('Game1','Game2'):
 with tempfile.TemporaryDirectory() as d:
  p=Path(d)
  active='TabType.Tab1' if game=='Game1' else 'TabType.Tab2'
  inactive='TabType.Tab2' if game=='Game1' else 'TabType.Tab1'
  (p/'Stubs.cs').write_text(stub.replace('GAME',game).replace('INACTIVE',inactive).replace('ACTIVE',active))
  sources=[p/'Stubs.cs',root/'Assets/Scripts/Assembly-CSharp'/game/'ChatTextField.cs',root/'Assets/Scripts/Assembly-CSharp'/game/'ipKeyboard.cs']
  refs=list((data/'NetStandard/ref/2.1.0').glob('*.dll'))
  args=['-nologo','-target:exe','-nostdlib+','-define:UNITY_WEBGL','-out:"'+str(p/'Check.dll')+'"']+['-r:"'+str(x)+'"' for x in refs]+['"'+str(x)+'"' for x in sources]
  (p/'Check.rsp').write_text('\n'.join(args),encoding='utf-8-sig')
  subprocess.run([str(dotnet),str(data/'DotNetSdkRoslyn/csc.dll'),'@'+str(p/'Check.rsp')],check=True)
  (p/'Check.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.0'},'rollForward':'LatestMajor'}}))
  subprocess.run([str(dotnet),str(p/'Check.dll')],check=True)
