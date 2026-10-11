"""Compile the actual imprisoned Wukong renderer in both client tabs."""
from pathlib import Path
import subprocess,tempfile
root=Path(__file__).resolve().parents[1]
def block(text,marker):
 start=text.index(marker);begin=text.index('{',start);end=begin+1;depth=1
 while depth:
  depth+=(text[end]=='{')-(text[end]=='}');end+=1
 return text[start:end]
for game in ('Game1','Game2'):
 source=(root/f'Assets/Scripts/Assembly-CSharp/{game}/Npc.cs').read_text(encoding='utf-8-sig')
 assert 'private void PaintNguHanhSeal(' in source,game+': imprisoned Wukong renderer missing'
 method=block(source,'private void PaintNguHanhSeal(')
 assert 'template.npcTemplateId == 48 && TileMap.mapID == 124' in source
 shell='''using System; using System.Collections.Generic;
class Image { }
class GameCanvas { public static int loads; public static Image loadImage(string path){if(path!="/mainimage/ngu_hanh_rock.png")throw new Exception("wrong rock path");loads++;return new Image();} }
class mGraphics { public const int BOTTOM=32,HCENTER=1; public int count,rocks,color; public void setColor(int c){color=c;} public void fillRect(int x,int y,int w,int h){if(w<=0||h<=0||w>12||h>24)throw new Exception("Oversized/blocky seal geometry");count++;Console.WriteLine("RECT|"+x+"|"+y+"|"+w+"|"+h+"|"+color);} public void drawImage(Image image,int x,int y,int anchor){if(image==null)throw new Exception("missing rock");rocks++;Console.WriteLine("ROCK|"+x+"|"+y);} }
class SmallImage { public static int count; public static void drawSmallImage(mGraphics g,int id,int x,int y,int dir,int anchor){if(id!=4468)throw new Exception("Portrait must be replaced by native head sprite4468");count++;Console.WriteLine("HEAD|"+x+"|"+y);} }
class Npc { int cx=1080,cy=288; static Image nguHanhRock; METHOD public void Run(){var g=new mGraphics();PaintNguHanhSeal(g);PaintNguHanhSeal(g);if(g.rocks!=2||SmallImage.count!=2||GameCanvas.loads!=1)throw new Exception("Rock/head renderer or caching");} }
class Test { static void Main(){new Npc().Run();Console.WriteLine("SEAL PASS");} }
'''.replace('METHOD',method)
 with tempfile.TemporaryDirectory() as temp:
  p=Path(temp);(p/'Test.cs').write_text(shell,encoding='utf8')
  subprocess.run(['C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe','/nologo','/out:'+str(p/'Test.exe'),str(p/'Test.cs')],check=True,capture_output=True)
  result=subprocess.run([str(p/'Test.exe')],check=True,capture_output=True,text=True)
  preview=root/'docs/ngu-hanh-seal-preview';preview.mkdir(parents=True,exist_ok=True)
  (preview/(game+'-draw-commands.txt')).write_text(result.stdout,encoding='utf8')
 print(game+' actual seal drawing compiled and passed')
for zoom in range(1,5):
 assert (root/f'Assets/Resources/res/x{zoom}/mainimage/ngu_hanh_rock.png').is_file(),'Native rock image missing zoom'+str(zoom)
