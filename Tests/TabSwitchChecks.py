from pathlib import Path
import re,subprocess,tempfile
root=Path(__file__).resolve().parents[1]
source=(root/'Assets/Scripts/Assembly-CSharp/Management.cs').read_text(encoding='utf-8-sig')
checks=[]
for game in ('Game1','Game2'):
 s=(root/f'Assets/Scripts/Assembly-CSharp/{game}/Main.cs').read_text(encoding='utf-8-sig')
 body=s[s.index('internal void checkInput()'):]
 guard=re.search(r'if \([^\n]+\) return;',body).group(0)
 checks.append('static bool Input'+game+'(){'+guard.replace('return;','return false;')+'return true;}')
shell="""using System;
namespace UnityEngine {public static class Time {public static int frameCount;}}
namespace UnityEngine.SceneManagement {public enum LoadSceneMode{Additive} public struct Scene{public string name;}public static class SceneManager{public static int sceneCount=2;public static Scene GetSceneAt(int i){return new Scene{name=i==0?"NROL":"NROL1"};}public static void LoadScene(string s,LoadSceneMode m){}}}
namespace NroWeb {public static class BrowserPlatform{public static bool IsBrowser=true;}public static class BrowserHost{public static int notifications;public static void TabChanged(int i){notifications++;}}}
public enum TabType {Tab1,Tab2}
class Check {METHODS static void Assert(bool ok,string s){if(!ok)throw new Exception(s);}static void Main(){
UnityEngine.Time.frameCount=10;Management.ChangeTab(1);
Assert(!InputGame1(),"Inactive tab receives input");Assert(!InputGame2(),"Same mouse release reaches new tab in the switch frame");
UnityEngine.Time.frameCount=11;Assert(InputGame2(),"Tab2 input never resumes");
Management.ChangeTab(0);Assert(!InputGame1(),"Return to tab1 replays switching click");
UnityEngine.Time.frameCount=12;Assert(InputGame1(),"Tab1 input never resumes");
Management.ChangeTab(0);Assert(InputGame1(),"Selecting current tab should not freeze input");
NroWeb.BrowserPlatform.IsBrowser=false;Management.ChangeTab(1);Assert(InputGame2(),"Native input behavior changed");
Console.WriteLine("PASS actual Main input guards and Management: switch-frame isolation, next-frame input, both directions, native preservation");}}
""".replace('METHODS','\n'.join(checks))
with tempfile.TemporaryDirectory() as d:
 p=Path(d);(p/'Management.cs').write_text(source);(p/'Checks.cs').write_text(shell)
 subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/out:'+str(p/'Checks.exe'),str(p/'Management.cs'),str(p/'Checks.cs')],check=True)
 subprocess.run([str(p/'Checks.exe')],check=True)
