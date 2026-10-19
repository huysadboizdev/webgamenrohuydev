using System;
using System.Collections.Generic;
using System.Diagnostics;
using Game2.Assets.src.g;
namespace Game2 {
internal static class BossNoticeBoard {
    const short Current=-32760,History=-32759,Refresh=-32758,Previous=-32757,Next=-32756,SystemNotices=-32755,QuestBosses=-32752,GeneralBosses=-32751;
    static List<GameInfo> system=new List<GameInfo>();
    static List<BossNoticeEntry> entries=new List<BossNoticeEntry>();
    static Dictionary<short,BossNoticeEntry> displayed=new Dictionary<short,BossNoticeEntry>();
    static int mode=-1,category=2,page,pages=1,sequence,pending,sentPage,sentCategory,sentMode;
    static bool categoriesSupported;
    static long revision;
    static Dictionary<string,List<BossNoticeEntry>> cache=new Dictionary<string,List<BossNoticeEntry>>();
    static long sent,received,serverNow,lastDetail;
    static bool wants,openedThisLogin;
    static string note="";
    internal static BossNoticeEntry Detail;
    static long Elapsed {get{return (long)(Stopwatch.GetTimestamp()*(1000.0/Stopwatch.Frequency));}}
    internal static long Now {get{return serverNow+System.Math.Max(0,Elapsed-received);}}
    internal static void Reset(){system.Clear();entries.Clear();displayed.Clear();mode=-1;category=2;pending=0;wants=false;openedThisLogin=false;categoriesSupported=false;revision=0;cache.Clear();Detail=null;serverNow=0;page=0;pages=1;sent=0;note="";}
    internal static void SystemReceived(){
        system.Clear();for(int i=0;i<Panel.vGameInfo.size();i++)system.Add((GameInfo)Panel.vGameInfo.elementAt(i));
        if(mode<0)Detail=null;Render();
    }
    static void Add(short id,string title,string content="") {Panel.vGameInfo.addElement(new GameInfo{id=id,main=title,content=content,hasRead=true});}
    static void Render(){
        Panel.vGameInfo.removeAllElements();displayed.Clear();
        if(mode<0){Add(QuestBosses,"Boss nhiệm vụ");Add(GeneralBosses,"Boss");Add(SystemNotices,"Thông báo hệ thống");foreach(var row in system)Panel.vGameInfo.addElement(row);return;}
        Add(QuestBosses,"Boss nhiệm vụ");Add(GeneralBosses,"Boss");Add(Current,(category==1?"Boss nhiệm vụ":"Boss")+" - Hiện tại");Add(History,(category==1?"Boss nhiệm vụ":"Boss")+" - Lịch sử");Add(Refresh,"Làm mới - trang "+(page+1)+"/"+pages);
        if(page>0)Add(Previous,"Trang trước");if(page+1<pages)Add(Next,"Trang sau");Add(SystemNotices,"Thông báo hệ thống");
        if(entries.Count==0)Add(-32754,pending!=0?"\u0110ang c\u1eadp nh\u1eadt danh s\u00e1ch Boss":"Ch\u01b0a c\u00f3 d\u1eef li\u1ec7u Boss",note);
        if(serverNow>0)Add(-32753,"C\u1eadp nh\u1eadt "+DateTimeOffset.FromUnixTimeMilliseconds(serverNow).ToLocalTime().ToString("HH:mm:ss"),note);else if(!string.IsNullOrEmpty(note))Add(-32753,"Th\u00f4ng tin d\u1eef li\u1ec7u",note);
        for(int i=0;i<entries.Count;i++){short id=(short)(-31000-i);var row=entries[i];displayed[id]=row;Add(id,row.Summary(Now),row.Details(Now));}
    }
    internal static int Select(short id){
        if(id==QuestBosses||id==GeneralBosses||id==Current||id==History){
            if(id==QuestBosses||id==GeneralBosses){category=id==QuestBosses?1:2;mode=0;}else mode=id==Current?0:1;
            pending=0;page=0;pages=1;openedThisLogin=true;LoadCache();Detail=null;note="";wants=true;Request();Render();return 1;
        }
        if(id==SystemNotices){mode=-1;pending=0;wants=false;Detail=null;Render();return 1;}
        if(id==Refresh||id==Previous||id==Next){if(id==Previous)page=System.Math.Max(0,page-1);if(id==Next)page=System.Math.Min(pages-1,page+1);Detail=null;wants=true;Request();Render();return 1;}
        BossNoticeEntry row;if(displayed.TryGetValue(id,out row)){Detail=row;return 2;}
        if(id<=-32751)return 2;
        Detail=null;return 0;
    }
    static void Request(){
        if(mode<0||pending!=0||Elapsed-sent<2100)return;
        wants=false;pending=++sequence;if(sequence<=0){sequence=1;pending=1;}sent=Elapsed;sentPage=page;sentCategory=category;sentMode=mode;
        Service.gI().bossNotices(pending,(sbyte)category,(sbyte)mode,page,true);
    }
    internal static void Tick(Panel panel){
        if(!panel.isShow||panel.type!=23||mode<0)return;
        if(pending!=0&&Elapsed-sent>10000){pending=0;note="Chưa nhận được dữ liệu. Chọn Làm mới để thử lại.";wants=false;Render();if(panel.type==23)panel.RefreshBossList();}
        if(wants)Request();
    }
    internal static void Receive(Message msg){
        var r=msg.reader();int marker=r.readInt();bool push=marker==-3;bool v2=marker==-2||push;int id=push?0:(v2?r.readInt():marker);int incomingCategory=push?r.readUnsignedByte():(v2?r.readUnsignedByte():2);int kind=r.readUnsignedByte();int receivedPage=r.readUnsignedShort();int receivedPages=r.readUnsignedShort();long incomingRevision=push?r.readLong():0;
        long time=r.readLong();string warning=r.readUTF();int count=r.readUnsignedShort();
        if(count>20||(v2&&(incomingCategory<1||incomingCategory>2))||kind>1||receivedPage>=receivedPages||receivedPages<1||receivedPages>500)throw new Exception("Invalid boss notice packet");
        var rows=new List<BossNoticeEntry>();for(int i=0;i<count;i++){
            var row=new BossNoticeEntry{Id=r.readUTF(),Name=r.readUTF(),Map=r.readUTF(),Killer=r.readUTF(),Status=r.readUTF(),Appeared=r.readLong(),Defeated=r.readLong(),Next=r.readLong(),Eta=r.readUTF()};
            if(v2){row.Category=r.readUnsignedByte();row.SemanticId=r.readInt();row.Support=r.readByte()!=0;row.Zone=r.readShort();if(row.Category!=incomingCategory||row.Zone < -1)throw new Exception("Invalid boss category row");}rows.Add(row);
        }
        if(push){if(incomingRevision<=revision)return;revision=incomingRevision;ApplySnapshot(incomingCategory,kind,receivedPage,rows,time,receivedPages,warning);return;}
        if(id==0){categoriesSupported=v2;serverNow=time;received=Elapsed;return;}
        if(id!=pending)return;
        bool match=BossNoticeRequestKey.Matches(pending,category,mode,page,id,incomingCategory,kind,sentPage)&&sentCategory==category&&sentMode==mode;
        pending=0;
        if(!match){wants=true;return;}
        serverNow=time;received=Elapsed;categoriesSupported=v2;
        if(!v2)warning="Server cũ chưa phân loại: danh sách Boss gồm cả boss nhiệm vụ. "+warning;
        entries=rows;page=receivedPage;pages=receivedPages;note=warning;cache[Key(category,kind,page)]=new List<BossNoticeEntry>(rows);
        Render();if(GameCanvas.panel.type==23)GameCanvas.panel.RefreshBossList();
    }
    internal static string DetailText(){if(Detail==null)return null;long now=Elapsed;if(now-lastDetail<1000)return null;lastDetail=now;return Detail.Details(Now);}
    static string Key(int c,int k,int p){return c+":"+k+":"+p;}
    static void LoadCache(){List<BossNoticeEntry> saved;if(cache.TryGetValue(Key(category,mode,page),out saved))entries=new List<BossNoticeEntry>(saved);else entries.Clear();}
    static void ApplySnapshot(int c,int k,int p,List<BossNoticeEntry> rows,long time,int pageCount,string warning){cache[Key(c,k,p)]=new List<BossNoticeEntry>(rows);if(c!=category||k!=mode)return;pending=0;wants=false;serverNow=time;received=Elapsed;entries=rows;page=p;pages=pageCount;note=warning;if(GameCanvas.panel.type==23){Render();GameCanvas.panel.RefreshBossList();}}
    internal static void TransportDisconnected(){pending=0;wants=false;}
    internal static void Reconnected(){if(!openedThisLogin||mode<0)return;pending=0;sent=Elapsed-2100;wants=true;Request();}
    internal static void Opened(){if(mode<0)return;if(pending!=0&&Elapsed-sent>10000)pending=0;if(pending==0){wants=true;Request();}}
}
}
