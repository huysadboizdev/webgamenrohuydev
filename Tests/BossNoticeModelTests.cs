using System;
public static class BossNoticeModelTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run(){
        Check(BossNoticeTime.Age(1000000,1120000)=="2 phút trước","age from server clock");
        Check(BossNoticeTime.Age(1200000,1120000)=="vừa xong","clock skew never negative");
        var row=new BossNoticeEntry {Name="Fide",Map="Núi",Killer="admin",Status="DEFEATED",Appeared=1000000,Defeated=1120000,Next=1420000,Eta="COOLDOWN"};
        string detail=row.Details(1240000);
        Check(detail.Contains("admin")&&detail.Contains("2 phút trước")&&detail.Contains("3 phút nữa")&&detail.Contains("Núi"),"killer map age ETA");
        Check(!detail.Contains("khu")&&!detail.Contains("Khu"),"no zone details");
        row.Next=0;row.Eta="CONDITION";Check(row.Details(1240000).Contains("Chờ điều kiện"),"conditional ETA unknown");
        row.Status="DEPARTED";row.Defeated=0;Check(!row.Details(1240000).Contains("Người hạ"),"despawn is not defeat");
        row.Status="PREVIOUS_RUN";Check(row.Details(1240000).Contains("phiên server trước"),"restart never claims old alive");
        UnityEngine.Debug.Log("PASS Boss notice model: server age/countdown, killer/map, no zones, conditional ETA, despawn, previous run");
    }
}
