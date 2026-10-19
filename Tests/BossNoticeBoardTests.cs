using System;
using System.Reflection;
using System.Runtime.Serialization;
public static class BossNoticeBoardTests {
    const BindingFlags Fields=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
    static void Check(bool ok,string m){if(!ok)throw new Exception(m);}
    public static void Run(){
        var assembly=typeof(Game1.Panel).Assembly;var first=assembly.GetType("Game1.BossNoticeBoard",true);var second=assembly.GetType("Game2.BossNoticeBoard",true);
        first.GetMethod("Reset",Fields).Invoke(null,null);second.GetMethod("Reset",Fields).Invoke(null,null);
        first.GetField("pending",Fields).SetValue(null,99);second.GetField("pending",Fields).SetValue(null,100);
        first.GetMethod("Reset",Fields).Invoke(null,null);
        Check((int)second.GetField("pending",Fields).GetValue(null)==100,"first account reset cannot modify second tab request");
        Check(!object.ReferenceEquals(first.GetField("entries",Fields).GetValue(null),second.GetField("entries",Fields).GetValue(null)),"two account tabs own separate row lists");
        second.GetMethod("Reset",Fields).Invoke(null,null);
        foreach(string ns in new[]{"Game1","Game2"})Verify(ns);UnityEngine.Debug.Log("PASS both boss boards: lazy BOSS2 requests, categories/support/zone, stale category/page protection, push cache, reset, reconnect refresh, no periodic polling");}
    static void Verify(string ns){
        var assembly=typeof(Game1.Panel).Assembly;
        Type board=assembly.GetType(ns+".BossNoticeBoard",true),panelType=assembly.GetType(ns+".Panel",true),canvas=assembly.GetType(ns+".GameCanvas",true);
        Action<string,object> set=(n,v)=>board.GetField(n,Fields).SetValue(null,v);
        Func<string,object> get=n=>board.GetField(n,Fields).GetValue(null);
        Action reset=()=>board.GetMethod("Reset",Fields).Invoke(null,null);
        reset();object oldPanel=canvas.GetField("panel").GetValue(null);
        var panel=FormatterServices.GetUninitializedObject(panelType);canvas.GetField("panel").SetValue(null,panel);
        try{
            board.GetMethod("Receive",Fields).Invoke(null,new[]{Packet(ns,0,0,true,2,0)});
            Check((bool)get("categoriesSupported"),"legacy bootstrap response updates capability only");
            reset();
            set("mode",1);set("category",2);set("sentCategory",2);set("sentMode",1);set("pending",41);set("page",1);set("sentPage",0);
            Action<int,int> receive=(id,page)=>board.GetMethod("Receive",Fields).Invoke(null,new[]{Packet(ns,id,page)});
            receive(41,0);Check((int)get("page")==1&&(bool)get("wants")&&(int)get("pending")==0,"superseded page response preserves requested page");
            set("pending",42);set("sentPage",1);receive(41,0);Check((int)get("pending")==42,"stale request cannot replace pending request");
            receive(42,1);Check((int)get("pending")==0&&(int)get("page")==1,"valid response applies");
            var first=(BossNoticeEntry)((System.Collections.IList)get("entries"))[0];
            Check(first.Killer=="admin"&&first.Map=="Núi"&&first.Next==1420000,"actual protocol decoded correctly");
            string text=first.Details(1240000);Check(!text.Contains("Khu")&&!text.Contains("khu"),"no zones in client detail");
            panelType.GetField("isShow").SetValue(panel,true);panelType.GetField("type").SetValue(panel,23);
            set("wants",false);set("sent",0L);
            board.GetMethod("Tick",Fields).Invoke(null,new[]{panel});
            Check(!(bool)get("wants")&&(int)get("pending")==0,"idle list never starts periodic refresh");
            set("sent",long.MaxValue);
            board.GetMethod("Opened",Fields).Invoke(null,null);
            Check((bool)get("wants")&&(int)get("pending")==0,"opening requests fresh data even during throttle wait");
            set("pending",44);set("sent",0L);
            board.GetMethod("Opened",Fields).Invoke(null,null);
            Check((int)get("pending")!=44,"reopening abandons an expired request and starts a fresh one");
            set("pending",0);set("sent",long.MaxValue);set("wants",true);
            panelType.GetField("type").SetValue(panel,24);set("sent",0L);
            board.GetMethod("Tick",Fields).Invoke(null,new[]{panel});
            Check((int)get("pending")==0,"reading detail never starts queued request");
            set("Detail",first);set("pending",43);receive(43,1);
            Check(object.ReferenceEquals(get("Detail"),first),"late response cannot replace detail being read");
            set("mode",0);set("category",1);set("sentCategory",1);set("sentMode",0);set("page",0);set("sentPage",0);set("pending",80);
            board.GetMethod("Receive",Fields).Invoke(null,new[]{Packet(ns,79,0,true,2,0)});
            Check((int)get("pending")==80,"previous category response cannot cancel new pending request");
            board.GetMethod("Receive",Fields).Invoke(null,new[]{Packet(ns,80,0,true,1,0)});
            first=(BossNoticeEntry)((System.Collections.IList)get("entries"))[0];
            Check(first.Category==1&&first.Zone==7&&first.Support&&!first.Details(1240000).Contains("Khu:"),"BOSS2 semantic category support and public zone decoded");
            board.GetMethod("Receive",Fields).Invoke(null,new[]{Packet(ns,0,0,true,1,0,true,5)});
            Check((long)get("revision")==5&&((System.Collections.IDictionary)get("cache")).Contains("1:0:0"),"push snapshot updates hidden query cache");
            board.GetMethod("Receive",Fields).Invoke(null,new[]{Packet(ns,0,0,true,1,0,true,4)});
            Check((long)get("revision")==5,"stale push revision is ignored");
            set("pending",81);set("category",2);set("page",0);set("sentPage",0);
            board.GetMethod("Receive",Fields).Invoke(null,new[]{Packet(ns,81,0,true,1,0)});
            Check((bool)get("wants")&&((System.Collections.IList)get("entries"))[0]==first,"changed category rejects matching old request");
            int oldSequence=(int)get("sequence");reset();Check((int)get("sequence")==oldSequence,"account reset never reuses sequence IDs");
            Check(!(bool)get("categoriesSupported"),"account reset clears server capability");
            reset();Check((int)get("mode")==-1&&(int)get("pending")==0&&((System.Collections.IList)get("entries")).Count==0,"reconnect reset removes prior state");
        }finally{canvas.GetField("panel").SetValue(null,oldPanel);reset();}
    }
    static object Packet(string ns,int id,int page,bool v2=false,int category=2,int kind=1,bool push=false,long revision=0){
        var assembly=typeof(Game1.Panel).Assembly;Type message=assembly.GetType(ns+".Message",true);
        object outgoing=Activator.CreateInstance(message,new object[]{-58});object w=message.GetMethod("writer").Invoke(outgoing,null);
        Action<string,object> write=(n,v)=>{
            if(n=="writeLong"){
                long number=(long)v;var method=w.GetType().GetMethod("writeInt",new[]{typeof(int)});
                method.Invoke(w,new object[]{(int)(number>>32)});method.Invoke(w,new object[]{unchecked((int)number)});
            }else w.GetType().GetMethod(n,new[]{v.GetType()}).Invoke(w,new[]{v});
        };
        if(push){write("writeInt",-3);write("writeByte",(sbyte)category);write("writeByte",(sbyte)kind);write("writeShort",page);write("writeShort",2);write("writeLong",revision);}else if(v2){write("writeInt",-2);write("writeInt",id);write("writeByte",(sbyte)category);}else write("writeInt",id);if(!push){write("writeByte",(sbyte)kind);write("writeShort",page);write("writeShort",2);}write("writeLong",1240000L);write("writeUTF","");write("writeShort",1);
        foreach(string s in new[]{"spawn","Fide","Núi","admin","DEFEATED"})write("writeUTF",s);
        write("writeLong",1000000L);write("writeLong",1120000L);write("writeLong",1420000L);write("writeUTF","COOLDOWN");
        if(v2){write("writeByte",(sbyte)category);write("writeInt",-20);write("writeByte",(sbyte)1);write("writeShort",7);}
        return Activator.CreateInstance(message,new object[]{(sbyte)-58,message.GetMethod("getData").Invoke(outgoing,null)});
    }
}
