public static class BossScreenNoticeData
{
	public const int ScreenCommand = 115;

	public const sbyte ScreenSchema = 1;

	public const sbyte ScreenVersion = 1;

	public const int BossDrabura = -233;

	public const int BossBuiBui = -234;

	public const int BossYaCon = -235;

	public const int BossMabu12H = -236;

	public const int BossDrabura2 = -237;

	public const int BossBuiBui2 = -238;

	public const int BossDrabura3 = -343;

	public const int BossGoku = -341;

	public const int BossCadic = -342;

	public const int OshinMapIdMin = 114;

	public const int OshinMapIdMax = 120;

	public const int BossMabu = -214;

	public const int BossSuperBu = -348;

	public const int MapTypeMabu14H = 9;

	public const int MapTypeDoanhTrai = 2;

	public const int MapTypeBanDoKhoBau = 4;

	public const int MapTypeConDuongRanDoc = 6;

	public const int MapTypeKhiGasHuyDiet = 7;

	public const int BossAnTrom = -365;

	public const int BossODo = -78;

	public const int BossSoiHecQuyn = -77;

	public const int BossMatTroi = -799;

	public class BossScreenEvent
	{
		public readonly string eventKey;

		public readonly int semanticBossId;

		public readonly int form;

		public readonly int mapId;

		public readonly int mapType;

		public readonly string ticker;

		public BossScreenEvent(string eventKey, int semanticBossId, int form, int mapId, int mapType, string ticker)
		{
			this.eventKey = eventKey;
			this.semanticBossId = semanticBossId;
			this.form = form;
			this.mapId = mapId;
			this.mapType = mapType;
			this.ticker = ticker;
		}
	}

	public static bool ShouldShow(int semanticBossId, int mapId, int mapType)
	{
		return !Hide(semanticBossId, mapId, mapType);
	}

	public static bool Hide(int semanticBossId, int mapId, int mapType)
	{
		if (IsSpecialBoss(semanticBossId))
		{
			return true;
		}
		if (IsTwelveHourBoss(semanticBossId) && mapId >= OshinMapIdMin && mapId <= OshinMapIdMax)
		{
			return true;
		}
		if ((semanticBossId == BossMabu || semanticBossId == BossSuperBu) && mapType == MapTypeMabu14H)
		{
			return true;
		}
		if (IsDungeonMapType(mapType))
		{
			return true;
		}
		return false;
	}

	private static bool IsTwelveHourBoss(int semanticBossId)
	{
		switch (semanticBossId)
		{
		case BossDrabura:
		case BossBuiBui:
		case BossYaCon:
		case BossMabu12H:
		case BossDrabura2:
		case BossBuiBui2:
		case BossDrabura3:
		case BossGoku:
		case BossCadic:
			return true;
		default:
			return false;
		}
	}

	private static bool IsSpecialBoss(int semanticBossId)
	{
		switch (semanticBossId)
		{
		case BossAnTrom:
		case BossODo:
		case BossSoiHecQuyn:
		case BossMatTroi:
			return true;
		default:
			return false;
		}
	}

	private static bool IsDungeonMapType(int mapType)
	{
		switch (mapType)
		{
		case MapTypeDoanhTrai:
		case MapTypeBanDoKhoBau:
		case MapTypeConDuongRanDoc:
		case MapTypeKhiGasHuyDiet:
			return true;
		default:
			return false;
		}
	}
}
