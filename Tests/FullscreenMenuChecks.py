"""Execute the actual settings menu methods without a Unity/application build."""
from pathlib import Path
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]


def method(source, signature):
    begin = source.index(signature)
    cursor = source.index('{', begin) + 1
    level = 1
    while level:
        level += (source[cursor] == '{') - (source[cursor] == '}')
        cursor += 1
    return source[begin:cursor]


parts = []
for game in ['Game1', 'Game2']:
    base = root / 'Assets/Scripts/Assembly-CSharp' / game
    labels = method((base / 'SoundMn.cs').read_text(encoding='utf-8-sig'), 'public void getStrOption()')
    fire = method((base / 'Panel.cs').read_text(encoding='utf-8-sig'), 'private void doFireOption()').replace('private void', 'public void', 1)
    code = r'''namespace GAME {
public static class GameCanvas {public static bool isLoadRes=true,isPlaySound,isTouch=true,lowGraphic;}
public static class GameScr {public static int isAnalog;public static bool isPaintChatVip;}
public static class Char {public static bool isPaintAura,isPaintAura2;}
public static class mResources {public static string turnOffAnalog="Analog",turnOnAnalog="Analog",serverchat_off="Chat",aura_off="Aura",aura_off_2="Aura2",turnOffSound="Sound",cauhinhthap="Graphics";}
public class SoundMn {static SoundMn instance=new SoundMn();public static SoundMn gI(){return instance;} public static int analog;
public void AuraToolOption(){}public void AuraToolOption2(){}public void soundToolOption(){}public void CaseSizeScr(){}public void CaseAnalog(){analog++;}
LABELS }
public class Panel {public static string[] strCauhinh; public int selected; FIRE }
public static class Check {public static void Run(){
 NroWeb.BrowserPlatform.CanOfferFullscreen=false;SoundMn.gI().getStrOption();if(Panel.strCauhinh.Length!=5)throw new System.Exception("GAME non-Android settings changed");
 NroWeb.BrowserPlatform.CanOfferFullscreen=true;SoundMn.gI().getStrOption();if(Panel.strCauhinh.Length!=6||Panel.strCauhinh[5]!="Toàn màn hình")throw new System.Exception("GAME Android fullscreen setting missing");
 int calls=NroWeb.BrowserPlatform.calls;var panel=new Panel{selected=5};panel.doFireOption();if(NroWeb.BrowserPlatform.calls!=calls+1||SoundMn.analog!=0)throw new System.Exception("GAME fullscreen setting triggered another game option");
 panel.selected=4;panel.doFireOption();if(SoundMn.analog!=1)throw new System.Exception("GAME native analog option changed");
 NroWeb.BrowserPlatform.CanOfferFullscreen=false;SoundMn.gI().getStrOption();if(Panel.strCauhinh.Length!=5)throw new System.Exception("GAME fullscreen remained in iOS/PC menu");
}}
}'''.replace('GAME', game).replace('LABELS', labels).replace('FIRE', fire)
    parts.append(code)

with tempfile.TemporaryDirectory() as folder:
    script = Path(folder) / 'check.ps1'
    script.write_text("$ErrorActionPreference='Stop'\nAdd-Type -TypeDefinition @'\nnamespace NroWeb {public static class BrowserPlatform {public static bool CanOfferFullscreen;public static int calls;public static void ShowFullscreenMenu(){calls++;}}}\n" + '\n'.join(parts) + "\n'@\n[Game1.Check]::Run()\n[Game2.Check]::Run()\nWrite-Output 'PASS Android fullscreen settings action; iOS/PC native menu and analog preserved, both tabs'\n", encoding='utf-8-sig')
    result = subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', str(script)], capture_output=True, text=True)
    print(result.stdout)
    print(result.stderr)
    raise SystemExit(result.returncode)
