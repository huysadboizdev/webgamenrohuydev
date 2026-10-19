namespace Game1.Mod.XMAP
{
	public class AutoXmap
	{
		public static bool IsXmapRunning = false;

		public static bool IsMapTransAsXmap = false;

		public static bool IsShowPanelMapTrans = true;

		public static bool IsUseCapsuleNormal = true;

		public static bool IsUseCapsuleVip = true;

		public static int IdMapCapsuleReturn = -1;

		private const long StaleCapsulePanelTimeout = 30000L;
		private static bool IgnoreNextCapsulePanelResponse;
		private static long IgnoreCapsulePanelResponseUntil;

		public static void Update()
		{
			if (XmapData.GI().IsLoading)
			{
				XmapData.GI().Update();
			}
			if (IsXmapRunning)
			{
				XmapController.Update();
			}
		}

		public static void Info(string text)
		{
			string lower = text.ToLower();
			bool capsuleRequestPending = XmapData.GI().IsCapsuleLoading || XmapController.HasPendingCapsuleSelection;
			if (capsuleRequestPending && (lower.Contains("kh\u00f4ng th\u1ec3 v\u00e0o \u0111\u01b0\u1ee3c khu") || lower.Contains("kh\u00f4ng th\u1ec3 mang ng\u1ecdc r\u1ed3ng")))
			{
				XmapController.FinishXmap();
			}
			if (text.Equals("Bạn chưa thể đến khu vực này"))
			{
				XmapController.FinishXmap();
			}
			if ((text.ToLower().Contains("chức năng bảo vệ") || text.ToLower().Contains("đã hủy xmap")) && IsXmapRunning)
			{
				XmapController.FinishXmap();
			}
		}

		public static void SelectMapTrans(int selected)
		{
			if (IsMapTransAsXmap)
			{
				XmapController.HideInfoDlg();
				XmapController.StartRunToMapId(XmapData.GetIdMapFromPanelXmap(GameCanvas.panel.mapNames[selected]));
			}
			else
			{
				XmapController.SaveIdMapCapsuleReturn();
				Service.gI().requestMapSelect(selected);
			}
		}

		public static bool CanRequestCapsuleMapList()
		{
			if (!IgnoreNextCapsulePanelResponse)
			{
				return true;
			}
			if (mSystem.currentTimeMillis() < IgnoreCapsulePanelResponseUntil)
			{
				return false;
			}
			IgnoreNextCapsulePanelResponse = false;
			return true;
		}

		public static void IgnorePendingCapsulePanelResponse()
		{
			IgnoreNextCapsulePanelResponse = true;
			IgnoreCapsulePanelResponseUntil = mSystem.currentTimeMillis() + StaleCapsulePanelTimeout;
		}

				public static void ShowPanelMapTrans()
		{
			IsMapTransAsXmap = false;
			if (IgnoreNextCapsulePanelResponse)
			{
				if (mSystem.currentTimeMillis() < IgnoreCapsulePanelResponseUntil)
				{
					IgnoreNextCapsulePanelResponse = false;
					IsShowPanelMapTrans = true;
					return;
				}
				IgnoreNextCapsulePanelResponse = false;
			}
			if (IsShowPanelMapTrans)
			{
				GameCanvas.panel.setTypeMapTrans();
				GameCanvas.panel.show();
			}
			else
			{
				IsShowPanelMapTrans = true;
			}
		}

		public static void FixBlackScreen()
		{
			Controller.gI().loadCurrMap(0);
			Service.gI().finishLoadMap();
			Char.isLoadingMap = false;
		}
	}
}
