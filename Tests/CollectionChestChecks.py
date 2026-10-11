"""Compile actual client mode/routing methods with a minimal panel shell, both tabs.
Also verify the NPC's isolated drawing branch and packet integration at source level.
Run: python Tests/CollectionChestChecks.py
"""
import pathlib, subprocess, tempfile

ROOT = pathlib.Path(__file__).resolve().parents[1]
CSC = pathlib.Path('C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe')

def block(source, marker):
    start = source.index(marker)
    begin = source.index('{', start)
    depth = 1
    end = begin + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]

for game in ('Game1', 'Game2'):
    folder = ROOT / 'Assets/Scripts/Assembly-CSharp' / game
    npc = (folder / 'Npc.cs').read_text(encoding='utf-8-sig')
    assert 'template.npcTemplateId == 82' in npc, game + ': collection chest still rendered as humanoid'
    chest = block(npc, 'else if (template.npcTemplateId == 82)')
    assert chest.count('SmallImage.drawSmallImage(') == 1, game + ': chest must draw once'
    assert '15288, cx, cy, 0, mGraphics.BOTTOM | mGraphics.HCENTER' in chest
    assert 'cy - ch - 7' in chest and 'dyEff = ch + 33' in chest
    assert 'ch = 29;' in npc
    panel = (folder / 'Panel.cs').read_text(encoding='utf-8-sig')
    controller = (folder / 'Controller.cs').read_text(encoding='utf-8-sig')
    assert 'isCollectionBox = false;' in block(panel, 'public void setTypeBox()')
    assert panel.count('getItem(BoxTransferType(') == 4, game + ': all box transfer paths covered'
    packet = block(controller, 'if (b18 == 1)')
    assert 'boxMarker == 1' in packet and 'boxMarker == 2' in packet
    assert packet.index('setTypeBox();') < packet.index('setCollectionBoxMode(') < packet.index('panel.show();')
    methods = block(panel, 'public void setCollectionBoxMode(') + '\n' + block(panel, 'private sbyte BoxTransferType(')
    shell = '''using System;
 class GameCanvas { public static Panel panel, panel2; }
class Panel {
 public int type; public bool isCollectionBox, isBoxClan;
 public string[][][] tabName = new string[8][][];
 const sbyte BOX_BAG=0, BAG_BOX=1, BODY_BOX=3;
 METHODS
 public sbyte Route(sbyte n) { return BoxTransferType(n); }
 public void setTypeBox() { type=2; isCollectionBox=isBoxClan=false; }
 public void show() {}
}
class Reader { public int marker; public sbyte readByte() { if(marker<0)throw new Exception(); return (sbyte)marker; } }
class Message { public Reader input=new Reader(); public Reader reader(){return input;} }
class Controller { public void Open(int marker) { int b18=1; Message msg=new Message(); msg.input.marker=marker; PACKET } }
class Char { public static Char instance=new Char(); public object npcFocus; public static Char myCharz(){return instance;} }
class ChatPopup { public static object currChatPopup; }
class Mob { public static object imgHP=new object(); }
class mGraphics {
 public const int BOTTOM=32, HCENTER=1; public int arrows, arrowX, arrowY;
 public void drawRegion(object img,int a,int b,int c,int d,int e,int x,int y,int anchor) { arrows++; arrowX=x; arrowY=y; }
}
class SmallImage {
 public static int calls,id,x,y,anchor;
 public static void drawSmallImage(mGraphics g,int i,int xx,int yy,int transform,int aa) {calls++;id=i;x=xx;y=yy;anchor=aa;}
}
class NpcVisual {
 public int cx=100,cy=200,ch=29,dyEff;
 public void Paint(mGraphics g) { CHEST }
}
class Tests {
 static void Check(bool x) { if(!x) throw new Exception("routing/mode regression"); }
 static void Main() {
  Panel p = new Panel { type=2 }; p.tabName[2]=new string[][] {new string[]{"Chest"},new string[]{"Bag"}};
  Panel secondary = new Panel {type=7}; GameCanvas.panel=p; GameCanvas.panel2=secondary;
  p.setCollectionBoxMode(true);
  Check(p.isCollectionBox && secondary.isCollectionBox && p.tabName[2][0][0]=="Sưu tầm");
  Check(p.tabName[2][1][0]=="Bag");
  foreach(Panel target in new Panel[]{p,secondary}) {
   Check(target.Route(0)==13 && target.Route(1)==11 && target.Route(3)==12);
   Check(target.Route(8)==8 && target.Route(9)==9 && target.Route(2)==2);
  }
  secondary.isCollectionBox=false; Check(secondary.Route(1)==11);
  p.setCollectionBoxMode(false); Check(!secondary.isCollectionBox);
  Check(p.Route(0)==0 && secondary.Route(1)==1 && p.Route(3)==3);
  p.setCollectionBoxMode(true); p.type=0;
  Check(p.Route(1)==1 && secondary.Route(1)==1);
  Controller controller=new Controller();
  foreach(int marker in new int[]{-1,0,1,2,99}) {
   controller.Open(marker);
   Check(p.isCollectionBox==(marker==2) && secondary.isCollectionBox==(marker==2));
   Check(p.isBoxClan==(marker==1));
  }
  NpcVisual npc=new NpcVisual(); mGraphics graphics=new mGraphics();
  npc.Paint(graphics); Check(SmallImage.calls==1 && graphics.arrows==0);
  Check(SmallImage.id==15288 && SmallImage.x==100 && SmallImage.y==200 && SmallImage.anchor==33);
  Char.instance.npcFocus=npc; npc.Paint(graphics);
  Check(SmallImage.calls==2 && graphics.arrows==1 && graphics.arrowX==100 && graphics.arrowY==164 && npc.dyEff==62);
  ChatPopup.currChatPopup=new object(); npc.Paint(graphics); Check(graphics.arrows==1 && SmallImage.calls==3);
  Console.WriteLine("PASS protocol routing, secondary fallback, ordinary box, pet2, mode reset, labels");
  Console.WriteLine("PASS markers missing/0/1/2/unknown and grounded single sprite, focus arrow, popup suppression");
 }
}'''.replace('METHODS', methods).replace('PACKET', packet).replace('CHEST', chest[chest.index('{') + 1:-1])
    with tempfile.TemporaryDirectory(prefix='chest-tests-') as tmp:
        cs = pathlib.Path(tmp) / 'Checks.cs'; exe = pathlib.Path(tmp) / 'Checks.exe'
        cs.write_text(shell, encoding='utf-8-sig')
        subprocess.run([str(CSC), '/nologo', '/out:' + str(exe), str(cs)], check=True)
        subprocess.run([str(exe)], check=True)
    print(game + ': PASS single grounded chest draw and packet integration')
