from pathlib import Path
import subprocess,json
root=Path(__file__).resolve().parents[1]
out=root/'validation-shell';out.mkdir(exist_ok=True)
data=Path('C:/Program Files/Unity/Hub/Editor/2022.3.62f2/Editor/Data')
source='''using System;
public enum TabType { Tab1,Tab2 }
public class Management { public static TabType tab; }
namespace UnityEngine { public enum TouchScreenKeyboardType { ASCIICapable,NumberPad } public class TouchScreenKeyboard { public static bool hideInput; public bool done; public string text; public static TouchScreenKeyboard Open(string text,TouchScreenKeyboardType type,bool autocorrection,bool multiline,bool secure,bool alert,string caption) { throw new Exception("Native keyboard reached"); } } }
namespace NroWeb { public static class BrowserPlatform { public static bool IsBrowser=true,HasCommandInput; public static string InputValue; public static int InputState; public static void OpenInput(string text,bool password,bool numeric,bool field=true) { InputValue=text; InputState=1; HasCommandInput=!field; } public static void CloseInput() { InputState=0; HasCommandInput=false; } } }
namespace Game1 { public class Command { public Action<string> action; public void perform(string text) { action(text); } } }
namespace Game2 { public class Command { public Action<string> action; public void perform(string text) { action(text); } } }
class Fixture {
 static void Assert(bool check,string message) { if(!check)throw new Exception(message); }
 static void Main() {
 int calls=0; string got=null;
 Management.tab=TabType.Tab1;
 Game1.ipKeyboard.openKeyBoard("chat",0,"hello",new Game1.Command { action=text=>{calls++;got=text;} });
 NroWeb.BrowserPlatform.InputValue="done text";NroWeb.BrowserPlatform.InputState=2;
 Game2.ipKeyboard.update();Assert(calls==0,"Inactive tab consumed Done");
 Game1.ipKeyboard.update();Game1.ipKeyboard.update();Assert(calls==1&&got=="done text","Command Done must dispatch exactly once");
 Game1.ipKeyboard.openKeyBoard("old",0,"old",new Game1.Command { action=text=>calls++ });
 Game1.ipKeyboard.CancelBrowser();Management.tab=TabType.Tab2;
 Game2.ipKeyboard.openKeyBoard("new",0,"new",new Game2.Command { action=text=>{calls++;got=text;} });
 NroWeb.BrowserPlatform.InputValue="tab2";NroWeb.BrowserPlatform.InputState=2;
 Game1.ipKeyboard.update();Game2.ipKeyboard.update();Assert(calls==2&&got=="tab2","Old tab retained pending action");
 Game2.ipKeyboard.openKeyBoard("command",0,"",new Game2.Command { action=text=>calls++ });
 NroWeb.BrowserPlatform.OpenInput("field",false,false);NroWeb.BrowserPlatform.InputState=2;
 Game2.ipKeyboard.update();Assert(calls==2,"Command stole field Done");
 string directory=System.IO.Path.Combine(AppContext.BaseDirectory,"saves");
 var a=new NroWeb.FileSaveStore(System.IO.Path.Combine(directory,"Tab1"));var b=new NroWeb.FileSaveStore(System.IO.Path.Combine(directory,"Tab2"));
 a.Write("same",new byte[]{1,2});b.Write("same",new byte[]{3});a.Clear();Assert(a.Read("same")==null&&b.Read("same")[0]==3,"Save clear crossed tabs");b.Delete("same");Assert(b.Read("same")==null,"Save delete failed");
 Console.WriteLine("PASS shell fixtures: inactive tab, Done exactly once, tab cancellation, input ownership, isolated saves");
 }
}'''
p=out/'fixtures.cs';p.write_text(source,encoding='utf-8')
refs=list((data/'NetStandard/ref/2.1.0').glob('*.dll'))
sources=[p,root/'Assets/Scripts/Assembly-CSharp/Game1/ipKeyboard.cs',root/'Assets/Scripts/Assembly-CSharp/Game2/ipKeyboard.cs',root/'Assets/Scripts/Assembly-CSharp/Web/FileSaveStore.cs']
lines=['-nologo','-target:exe','-nostdlib+','-define:UNITY_WEBGL','-out:"'+str(out/'fixtures.dll')+'"']+['-r:"'+str(x)+'"' for x in refs]+['"'+str(x)+'"' for x in sources]
rsp=out/'fixtures.rsp';rsp.write_text('\n'.join(lines),encoding='utf-8-sig')
dotnet=data/'NetCoreRuntime/dotnet.exe'
r=subprocess.run([str(dotnet),str(data/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],capture_output=True,text=True);print(r.stdout);r.check_returncode()
(out/'fixtures.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.0'},'rollForward':'LatestMajor'}}))
r=subprocess.run([str(dotnet),str(out/'fixtures.dll')],capture_output=True,text=True);print(r.stdout,r.stderr);r.check_returncode()
