using System;
using System.Reflection;
public static class BossNoticeCategoryTests {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(){
  Check(typeof(BossNoticeEntry).GetField("Category")!=null,"category field missing");
  Check(typeof(BossNoticeEntry).GetField("Zone")!=null,"world quest zone missing");
  Check(typeof(BossNoticeEntry).GetField("Support")!=null,"semantic support marker missing");
  var row=new BossNoticeEntry{Name="Kuku",Map="Núi",Status="ALIVE"};
  typeof(BossNoticeEntry).GetField("Category").SetValue(row,1);
  typeof(BossNoticeEntry).GetField("Zone").SetValue(row,7);
  typeof(BossNoticeEntry).GetField("Support").SetValue(row,true);
  string detail=row.Details(1000);
  Check(!detail.Contains("Khu:")&&detail.Contains("Hỗ trợ 14:00–17:00"),"quest detail zone and support");
  typeof(BossNoticeEntry).GetField("Zone").SetValue(row,-1);
  Check(!row.Details(1000).Contains("Khu:"),"historical unknown zone remains hidden");
  var key=typeof(BossNoticeEntry).Assembly.GetType("BossNoticeRequestKey",true);
  var match=key.GetMethod("Matches");
  Func<int,int,int,int,int,int,int,int,bool> matches=(a,b,c,d,e,f,g,h)=>(bool)match.Invoke(null,new object[]{a,b,c,d,e,f,g,h});
  Check(matches(8,1,0,2,8,1,0,2),"same selection accepted");
  Check(!matches(8,1,0,2,8,2,0,2)&&!matches(8,1,0,2,7,1,0,2)&&!matches(8,1,0,2,8,1,1,2)&&!matches(8,1,0,2,8,1,0,3),"old request category view page rejected");
 }
}
