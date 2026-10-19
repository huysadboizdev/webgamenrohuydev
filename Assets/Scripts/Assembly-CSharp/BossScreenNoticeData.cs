public static class BossScreenNoticeData
{
	public const int ScreenCommand = 115;
	public const sbyte ScreenSchema = 1;
	public const sbyte ScreenVersion = 1;
	public const int BossDrabura = -233, BossBuiBui = -234, BossYaCon = -235, BossMabu12H = -236, BossDrabura2 = -237, BossBuiBui2 = -238, BossDrabura3 = -343, BossGoku = -341, BossCadic = -342;
	public const int OshinMapIdMin = 114, OshinMapIdMax = 120, BossMabu = -214, BossSuperBu = -348, MapTypeMabu14H = 9;
	public const int MapTypeDoanhTrai = 2, MapTypeBanDoKhoBau = 4, MapTypeConDuongRanDoc = 6, MapTypeKhiGasHuyDiet = 7;
	public const int BossAnTrom = -365, BossODo = -78, BossSoiHecQuyn = -77, BossMatTroi = -799;
	public class BossScreenEvent
	{
		public readonly string eventKey; public readonly int semanticBossId, form, mapId, mapType; public readonly string ticker;
		public BossScreenEvent(string eventKey, int semanticBossId, int form, int mapId, int mapType, string ticker) { this.eventKey=eventKey; this.semanticBossId=semanticBossId; this.form=form; this.mapId=mapId; this.mapType=mapType; this.ticker=ticker; }
	}
	public static bool ShouldShow(int semanticBossId, int mapId, int mapType) { return !Hide(semanticBossId,mapId,mapType); }
	public static bool Hide(int semanticBossId, int mapId, int mapType)
	{
		if(IsSpecialBoss(semanticBossId))return true;
		if(IsTwelveHourBoss(semanticBossId))return true;
		if((semanticBossId==BossMabu||semanticBossId==BossSuperBu)&&mapType==MapTypeMabu14H)return true;
		if(IsDungeonMapType(mapType))return true;
		return false;
	}
	private static bool IsTwelveHourBoss(int id) { switch(id) { case BossDrabura: case BossBuiBui: case BossYaCon: case BossMabu12H: case BossDrabura2: case BossBuiBui2: case BossDrabura3: case BossGoku: case BossCadic:return true; default:return false; } }
	private static bool IsSpecialBoss(int id) { switch(id) { case BossAnTrom: case BossODo: case BossSoiHecQuyn: case BossMatTroi:return true; default:return false; } }
	private static bool IsDungeonMapType(int type) { switch(type) { case MapTypeDoanhTrai: case MapTypeBanDoKhoBau: case MapTypeConDuongRanDoc: case MapTypeKhiGasHuyDiet:return true; default:return false; } }
}
