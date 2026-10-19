using System;
using System.Text;

public static class BossNoticeTime
{
    public static string Age(long at,long now){
        long seconds=Math.Max(0,(now-at)/1000);
        if(seconds<60)return "vừa xong";
        if(seconds<3600)return seconds/60+" phút trước";
        if(seconds<86400)return seconds/3600+" giờ trước";
        return seconds/86400+" ngày trước";
    }
    public static string Stamp(long at){
        if(at<=0)return "Chưa xác định";
        try{return DateTimeOffset.FromUnixTimeMilliseconds(at).ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM HH:mm:ss");}
        catch{return "Chưa xác định";}
    }
}
public sealed class BossNoticeEntry
{
    public string Id,Name,Map,Killer,Status,Eta;
    public long Appeared,Defeated,Next;
    public int Category,SemanticId,Zone=-1;
    public bool Support;
    public string State {
        get {switch(Status){case "ALIVE":return "Đang xuất hiện";case "DEFEATED":return "Đã bị hạ";case "DEPARTED":return "Đã rời bản đồ";case "PREVIOUS_RUN":return "Lượt của phiên server trước";default:return "Chưa xác định";}}
    }
    public string Summary(long now){
        if(Status=="ALIVE")return Name+"\u00b7 S\u1ed1ng \u00b7 "+Map;
        if((Status=="DEFEATED"||Status=="DEPARTED")&&Next>now)return Name+"\u00b7 H\u1ed3i sinh sau "+Math.Max(1,(Next-now+59999)/60000)+" ph\u00fat";
        if(Eta=="CONDITION")return Name+"\u00b7 Ch\u1edd \u0111i\u1ec1u ki\u1ec7n";
        if(Eta=="SCHEDULE")return Name+"\u00b7 Ch\u1edd l\u1ecbch";
        return Name+"\u00b7 Ch\u01b0a r\u00f5 gi\u1edd h\u1ed3i sinh";
    }
    public string Details(long now){
        var text=new StringBuilder();text.Append(Name).Append('\n').Append("Trạng thái: ").Append(State).Append('\n');
        text.Append(Status=="ALIVE"?"Bản đồ: ":"Bản đồ lần này: ").Append(Map).Append('\n');
        if(Support)text.Append("Hỗ trợ 14:00–17:00").Append('\n');
        if(Appeared>0)text.Append("Xuất hiện: ").Append(BossNoticeTime.Stamp(Appeared)).Append(" (").Append(BossNoticeTime.Age(Appeared,now)).Append(")\n");
        if(Defeated>0){text.Append("Bị hạ: ").Append(BossNoticeTime.Stamp(Defeated)).Append(" (").Append(BossNoticeTime.Age(Defeated,now)).Append(")\n");text.Append("Người hạ: ").Append(string.IsNullOrEmpty(Killer)?"Không xác định":Killer).Append('\n');}
        if(Status!="ALIVE"){
            if(Next>now)text.Append("Hồi sinh dự kiến: khoảng ").Append(Math.Max(1,(Next-now+59999)/60000)).Append(" phút nữa\n").Append("Mốc dự kiến: ").Append(BossNoticeTime.Stamp(Next));
            else if(Next>0)text.Append("Hồi sinh: đang chờ xuất hiện");
            else text.Append("Hồi sinh: ").Append(Eta=="CONDITION"?"Chờ điều kiện hoặc lịch boss":"Chưa xác định");
        }
        return text.ToString();
    }
}

public static class BossNoticeRequestKey {
 public static bool Matches(int pending,int category,int view,int page,int id,int incomingCategory,int incomingView,int requestedPage){
  return pending>0&&pending==id&&category==incomingCategory&&view==incomingView&&page==requestedPage;
 }
}
