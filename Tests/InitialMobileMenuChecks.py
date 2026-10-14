"""Execute production menu initialization in memory; no Unity build required."""
from pathlib import Path
import re
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]


def method(source, signature):
    start = source.index(signature)
    brace = source.index('{', start)
    depth, end = 1, brace + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


fixture = r'''
using System;
namespace NroWeb { public static class BrowserPlatform { public static bool IsBrowser; } }
namespace GAME {
// Legacy Windows PowerShell lacks Unity's string.Contains(char) overload.
public static class StringCompatibility {
 public static bool Contains(this string value, char character) { return value.IndexOf(character)>=0; }
}
public static class Main { public static bool isIPhone = true; }
public static class mGraphics { public static int zoomLevel = 3; }
public static class mScreen { public static int cmdW = CMD_W, cmdH = CMD_H; }
public static class GameCanvas { public static int w, h, hw, hh; }
public static class Rms {
 public static string account; public static bool guest;
 public static string loadRMSString(string key) { return key == "acc" ? account : null; }
 public static byte[] loadRMS(string key) { return guest ? new byte[1] : null; }
}
public static class mResources {
 public static string playNew="New", choitiep="Continue", playAcc="Account", change_account="Change";
}
public class Command {
 public int x,y,w=mScreen.cmdW,h=mScreen.cmdH,hw,idAction,type;
 public string caption;
 public Command(string caption, object target, int action, object parameter) {
  this.caption=caption; idAction=action;
 }
 COMMAND_SET_TYPE
}
public class Menu {
 int nCmdPlay; public static int ipSelect;
 Command[] cmd; Command cmdDownload=null,cmdDeleteRMS=null;
 INITIALIZE
 RESIZE
 public void Check(bool browser, string account, bool guest, int height) {
  NroWeb.BrowserPlatform.IsBrowser=browser; Rms.account=account; Rms.guest=guest;
  GameCanvas.w=446; GameCanvas.h=height; GameCanvas.hw=223; GameCanvas.hh=height/2;
  initCommand();
  bool saved=!string.IsNullOrEmpty(account)||guest;
  int[] expected=saved ? new[]{3,10100,7,17,8} : new[]{3,7,17,8};
  if(cmd.Length!=expected.Length) throw new Exception("Account-dependent command count changed");
  for(int i=0;i<cmd.Length;i++) {
   Command c=cmd[i];
   if(c.idAction!=expected[i]) throw new Exception("Account action/order changed");
   if(c.w!=160 || c.h!=mScreen.cmdH || c.type!=1) throw new Exception("Button size/type changed");
   if(c.x!=(GameCanvas.w-c.w)/2) throw new Exception("Button not centered");
   if(browser && (c.y<0 || c.y+c.h>height))
    throw new Exception("Cold-start "+cmd.Length+"-item menu clipped at logical height "+height+": command "+i+" bottom="+(c.y+c.h));
   if(!browser && c.y!=height/2-15*cmd.Length+28+i*30)
    throw new Exception("Native menu layout changed");
  }
  // Reinitialization on account switch must retain the same bounds.
  initCommand();
  if(browser && cmd[cmd.Length-1].y+cmd[cmd.Length-1].h>height)
   throw new Exception("Reinitialized menu clipped");
 }
}
public static class Check {
 public static void Run() {
  foreach(int height in new[]{174,188,210})
   foreach(bool browser in new[]{true,false}) {
    new Menu().Check(browser,null,false,height);
    new Menu().Check(browser,"",false,height);
    new Menu().Check(browser,"saved-account",false,height);
    new Menu().Check(browser,null,true,height);
   }
  Console.WriteLine("PASS GAME cold-start/reinitialized 4/5-item menus at 174/188/210; account actions and native layout preserved");
 }
}
}
'''

failures = []
for game in ('Game1', 'Game2'):
    base = root / 'Assets/Scripts/Assembly-CSharp' / game
    menu = (base / 'ServerListScreen.cs').read_text(encoding='utf8')
    screen = (base / 'mScreen.cs').read_text(encoding='utf8')
    command = (base / 'Command.cs').read_text(encoding='utf8')
    source = fixture.replace('COMMAND_SET_TYPE', method(command, 'public void setType()'))
    source = source.replace('INITIALIZE', method(menu, 'private void initCommand()'))
    source = source.replace('RESIZE', method(menu, 'public void ResizeBrowser()'))
    source = source.replace('CMD_W', re.search(r'cmdW = (\d+)', screen).group(1))
    source = source.replace('CMD_H', re.search(r'cmdH = (\d+)', screen).group(1))
    source = source.replace('GAME', game)
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / 'InitialMenu.cs'
        path.write_text(source, encoding='utf8')
        result = subprocess.run([
            'powershell', '-NoProfile', '-NonInteractive', '-Command',
            "$ErrorActionPreference='Stop'; Add-Type -Path '" + str(path).replace("'", "''")
            + "'; [" + game + '.Check]::Run()'
        ], text=True, capture_output=True)
        print(result.stdout.strip())
        if result.returncode:
            print(result.stderr.strip())
            failures.append(game)
if failures:
    raise SystemExit('FAIL initial mobile menu: ' + ', '.join(failures))
