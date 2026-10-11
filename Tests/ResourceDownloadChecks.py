from pathlib import Path
import re, subprocess, tempfile
root=Path(__file__).resolve().parents[1]
for game in ('Game1','Game2'):
    base=root/'Assets/Scripts/Assembly-CSharp'/game
    main=(base/'Main.cs').read_text(encoding='utf-8-sig')
    fixed=main[main.index('private void FixedUpdate()'):main.index('internal void checkInput()')]
    ordered=re.findall(r'(?:Session_ME(?:2)?\.update\(\)|GameMidlet\.gameCanvas\.update\(\));',fixed)
    controller=(base/'Controller.cs').read_text(encoding='utf-8-sig')
    progress=re.search(r'ServerListScreen.percent = [^;]*ServerListScreen\.nBig[^;]*;',controller).group(0)
    shell='''using System;
class ServerListScreen {public static int percent,demPercent,nBig;}
class Session_ME {public static void update(){if(!Ready)throw new Exception("Resource request before client zoom negotiation");}public static bool Ready;}
class Session_ME2 {public static void update(){}}
class Canvas {public void update(){Session_ME.Ready=true;}}
class GameMidlet {public static Canvas gameCanvas=new Canvas();}
class Checks {static void Step(){ORDER}static void Progress(){PROGRESS}static int Main(){try{
Step();
foreach(int total in new[]{0,-1,1434}){ServerListScreen.nBig=total;ServerListScreen.demPercent=1;Progress();if(ServerListScreen.percent<0||ServerListScreen.percent>100)throw new Exception("Invalid progress");}
ServerListScreen.nBig=1434;ServerListScreen.demPercent=1434;Progress();if(ServerListScreen.percent!=100)throw new Exception("Download should reach 100");
ServerListScreen.demPercent=2000;Progress();if(ServerListScreen.percent!=100)throw new Exception("Progress overflow");
Console.WriteLine("PASS GAME startup ordering, zero/negative totals, valid and saturated progress");return 0;}catch(Exception e){Console.Error.WriteLine(e.Message);return 1;}}}
'''.replace('ORDER','\n'.join(ordered)).replace('PROGRESS',progress).replace('GAME',game)
    with tempfile.TemporaryDirectory() as d:
        p=Path(d);(p/'Checks.cs').write_text(shell)
        subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/out:'+str(p/'Checks.exe'),str(p/'Checks.cs')],check=True)
        subprocess.run([str(p/'Checks.exe')],check=True)
