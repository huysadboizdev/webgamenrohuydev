using System.Collections.Generic;

namespace Game1.Mod.XMAP
{
	public class PickMobController
	{
		private enum TpyePickItem
		{
			CanNotPickItem = 0,
			PickItemNormal = 1,
			PickItemTDLT = 2,
			PickItemTanSat = 3
		}

		private static readonly sbyte[] IdSkillsMelee = new sbyte[5] { 0, 9, 2, 17, 4 };

		private static readonly sbyte[] IdSkillsCanNotAttack = new sbyte[5] { 10, 11, 14, 23, 7 };

		private static readonly PickMobController _Instance = new PickMobController();

		public static bool IsPickingItems;

		private static bool IsWait;

		private static long TimeStartWait;

		private static long TimeWait;

		private static int massacreSkillCursor;

		public static List<ItemMap> ItemPicks = new List<ItemMap>();

		private static int IndexItemPick = 0;

		public static void Update()
		{
			if (IsWaiting())
			{
				return;
			}
			Char @char = Char.myCharz();
			if (!CanAutoAct(@char))
			{
				return;
			}
			if (GameScr.hpPotion >= 1 && (@char.cHP <= @char.cHPFull * PickMob.HpBuff / 100 || @char.cMP <= @char.cMPFull * PickMob.MpBuff / 100))
			{
				GameScr.gI().doUseHP();
			}
			if (PickMob.IsAutoPickItems)
			{
				if (IsPickingItems)
				{
					if (IndexItemPick >= ItemPicks.Count)
					{
						IsPickingItems = false;
						Wait(100);
						return;
					}
					ItemMap itemMap = ItemPicks[IndexItemPick];
					if (GameScr.vItemMap.contains(itemMap))
					{
						Service.gI().pickItem(itemMap.itemMapID);
						itemMap.countAutoPick++;
					}
					Wait(500);
					IndexItemPick++;
				}
				ItemPicks.Clear();
				IndexItemPick = 0;
				for (int i = 0; i < GameScr.vItemMap.size(); i++)
				{
					ItemMap itemMap2 = (ItemMap)GameScr.vItemMap.elementAt(i);
					if (GetTypePickItem(itemMap2) != 0)
					{
						ItemPicks.Add(itemMap2);
					}
				}
				if (ItemPicks.Count > 0)
				{
					IsPickingItems = true;
					return;
				}
			}
			bool flag = ItemTime.isExistItem(4387);
			if (PickMob.tanSat)
			{
				if (@char.isCharge)
				{
					Wait(100);
					return;
				}
				if (@char.mobFocus != null && !IsMobTanSat(@char.mobFocus))
				{
					@char.mobFocus = null;
				}
                if (@char.mobFocus == null) @char.mobFocus = GetMobTanSat();
                if (CanStartMassacreSkill(@char))
                {
                    Skill skillAttack = GetSkillAttack();
                    Mob target = @char.mobFocus;
                    bool needsTarget = skillAttack != null && !skillAttack.template.isBuffToPlayer() && !skillAttack.template.isUseAlone();
                    if (skillAttack != null && (!needsTarget || target != null))
                    {
                        if (needsTarget && !IsWithinMassacreSkillRange(skillAttack, target.getX(), target.getY(), @char.cx, @char.cy))
                        {
                            int approachX = GetMassacreApproachX(skillAttack, target.getX(), @char.cx);
                            if (PickMob.telePem) TryMoveForAutoAttack(approachX, target.getY());
                            else Move(approachX, target.getY());
                            Wait(100);
                            return;
                        }
                        TryUseMassacreSkill(skillAttack, target, null);
                    }
                }
                if (@char.mobFocus == null && !flag)
				{
					Mob mobNext = GetMobNext();
					if (mobNext != null)
					{
						Char.myCharz().FocusManualTo(mobNext);
						if (PickMob.telePem)
						{
							if (Math.abs(mobNext.xFirst - @char.cx) >= 20 || Math.abs(mobNext.yFirst - @char.cy) >= 20)
							{
								TryMoveForAutoAttack(mobNext.xFirst, mobNext.yFirst);
							}
							return;
						}
						Move(mobNext.xFirst, mobNext.yFirst);
					}
				}
				Wait(100);
			}
			else
			{
				if (!PickMob.tsPlayer)
				{
					return;
				}
				if (@char.isCharge)
				{
					Wait(100);
					return;
				}
				if (@char.charFocus != null && !IsCharTanSat(@char.charFocus))
				{
					@char.charFocus = null;
				}
				if (@char.charFocus == null)
				{
					@char.charFocus = GetCharTanSat();
					if (@char.charFocus != null)
					{
						if (PickMob.telePem)
						{
							if (Math.abs(@char.charFocus.cx - @char.cx) >= 20 || Math.abs(@char.charFocus.cy - @char.cy) >= 20)
							{
								TryMoveForAutoAttack(@char.charFocus.cx, @char.charFocus.cy);
							}
							return;
						}
                        TryMoveForAutoAttack(@char.charFocus.cx, @char.charFocus.cy);
                        return;
					}
				}
				if (CanStartMassacreSkill(@char))
				{
					Skill skillAttack2 = GetSkillAttack();
					bool needsTarget = skillAttack2 != null && !skillAttack2.template.isBuffToPlayer() && !skillAttack2.template.isUseAlone();
					if (skillAttack2 != null && (!needsTarget || @char.charFocus != null))
					{
						Char charFocus = @char.charFocus;
						if (needsTarget && !IsWithinMassacreSkillRange(skillAttack2, charFocus.cx, charFocus.cy, @char.cx, @char.cy))
						{
							int approachX = GetMassacreApproachX(skillAttack2, charFocus.cx, @char.cx);
							if (PickMob.telePem)
							{
								TryMoveForAutoAttack(approachX, charFocus.cy);
								return;
							}
							Move(approachX, charFocus.cy);
							return;
						}
						TryUseMassacreSkill(skillAttack2, null, charFocus);
					}
				}
				Wait(100);
			}
		}

        private static bool CanAutoAct(Char player)
        {
            return player != null && Service.gI().IsCombatConnectionReady()
                && !Char.isLoadingMap && !Char.ischangingMap && !Controller.isStopReadMessage
                && !player.meDead && player.statusMe != 14 && player.statusMe != 5 && player.cHP > 0
                && !player.stone && !player.sleepEff && !player.blindEff && player.holdEffID == 0
                && !player.isTeleport && !player.telePortSkill && !player.isCreateDark
                && !player.isCharge && !player.isStandAndCharge && !player.isFlyAndCharge
                && !player.isUseSkillAfterCharge && !player.isWaitMonkey;
        }

        private static bool CanStartMassacreSkill(Char player)
        {
            SkillInfoPaint[] animation = player.skillInfoPaint();
            return animation == null || player.indexSkill >= animation.Length;
        }

        private static bool IsWithinMassacreSkillRange(Skill skill, int targetX, int targetY, int playerX, int playerY)
        {
            return skill != null && Math.abs(targetX - playerX) <= skill.dx
                && Math.abs(targetY - playerY) <= skill.dy;
        }

        private static int GetMassacreApproachX(Skill skill, int targetX, int playerX)
        {
            int offset = System.Math.Max(0, skill.dx - 20);
            return targetX + ((playerX > targetX) ? offset : -offset);
        }

        private static bool TryMoveForAutoAttack(int x, int y)
        {
            Char player = Char.myCharz();
            if (!CanAutoAct(player)) return false;
            y = System.Math.Max(1, y);
            int oldX = player.cx, oldY = player.cy;
            player.cx = x; player.cy = y;
            if (Service.gI().TryCharMove()) return true;
            player.cx = oldX; player.cy = oldY;
            return false;
        }

        private static bool EnsurePositionSent()
        {
            Char player = Char.myCharz();
            if (player.cx == player.cxSend && player.cy == player.cySend) return true;
            Service.gI().TryCharMove();
            Wait(100);
            return false;
        }

		public static void Move(int x, int y)
		{
			Char @char = Char.myCharz();
			if (!PickMob.vuotDiaHinh)
			{
				@char.currentMovePoint = new MovePoint(x, y);
				return;
			}
			int[] pointYsdMax = GetPointYsdMax(@char.cx, x);
			if (pointYsdMax[1] >= y || (pointYsdMax[1] >= @char.cy && (@char.statusMe == 2 || @char.statusMe == 1)))
			{
				pointYsdMax[0] = x;
				pointYsdMax[1] = y;
			}
			@char.currentMovePoint = new MovePoint(pointYsdMax[0], pointYsdMax[1]);
		}

		private static TpyePickItem GetTypePickItem(ItemMap itemMap)
		{
			Char @char = Char.myCharz();
			if (itemMap.playerId != @char.charID && itemMap.playerId != -1 && !PickMob.IsPickItemsAll)
			{
				return TpyePickItem.CanNotPickItem;
			}
			if (PickMob.IsLimitTimesPickItem && itemMap.countAutoPick > PickMob.TimesAutoPickItemMax)
			{
				return TpyePickItem.CanNotPickItem;
			}
			if (!FilterItemPick(itemMap))
			{
				return TpyePickItem.CanNotPickItem;
			}
			if (PickMob.IsPickItemsDis || (Res.abs(@char.cx - itemMap.xEnd) < 100 && Res.abs(@char.cy - itemMap.yEnd) < 100))
			{
				return TpyePickItem.PickItemNormal;
			}
			if (ItemTime.isExistItem(4387))
			{
				return TpyePickItem.PickItemTDLT;
			}
			if (PickMob.tanSat)
			{
				return TpyePickItem.PickItemTanSat;
			}
			return TpyePickItem.CanNotPickItem;
		}

		private static bool FilterItemPick(ItemMap itemMap)
		{
			if ((PickMob.IdItemPicks.Count == 0 || PickMob.IdItemPicks.Contains(itemMap.template.id)) && (PickMob.IdItemBlocks.Count == 0 || !PickMob.IdItemBlocks.Contains(itemMap.template.id)) && (PickMob.TypeItemPicks.Count == 0 || PickMob.TypeItemPicks.Contains(itemMap.template.type)))
			{
				if (PickMob.TypeItemBlock.Count != 0)
				{
					return !PickMob.TypeItemBlock.Contains(itemMap.template.type);
				}
				return true;
			}
			return false;
		}

		private static Mob GetMobTanSat()
		{
			Mob result = null;
			int num = int.MaxValue;
			Char @char = Char.myCharz();
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				int num2 = (mob.xFirst - @char.cx) * (mob.xFirst - @char.cx) + (mob.yFirst - @char.cy) * (mob.yFirst - @char.cy);
				if (IsMobTanSat(mob) && num2 < num)
				{
					result = mob;
					num = num2;
				}
			}
			return result;
		}

		private static Char GetCharTanSat()
		{
			Char result = null;
			for (int i = 0; i < GameScr.vCharInMap.size(); i++)
			{
				Char @char = (Char)GameScr.vCharInMap.elementAt(i);
				if (IsCharTanSat(@char))
				{
					result = @char;
				}
			}
			return result;
		}

		private static Mob GetMobNext()
		{
			Mob result = null;
			long num = mSystem.currentTimeMillis();
			for (int i = 0; i < GameScr.vMob.size(); i++)
			{
				Mob mob = (Mob)GameScr.vMob.elementAt(i);
				if (IsMobNext(mob) && mob.lastDie < num)
				{
					result = mob;
					num = mob.lastDie;
				}
			}
			return result;
		}

		private static bool IsMobTanSat(Mob mob)
		{
			if (mob == null || !GameScr.vMob.contains(mob) || !GameScr.gI().isMeCanAttackMob(mob)
                || mob.status == 0 || mob.status == 1 || mob.hp <= 0 || mob.isMobMe)
			{
				return false;
			}
			bool flag = PickMob.neSieuQuai && !ItemTime.isExistItem(4387);
			return (mob.levelBoss == 0 || !flag) && FilterMobTanSat(mob);
		}

		private static bool IsCharTanSat(Char c)
		{
			return Char.myCharz().isMeCanAttackOtherPlayer(c);
		}

		private static bool IsMobNext(Mob mob)
		{
			if (mob.isMobMe)
			{
				return false;
			}
			if (!FilterMobTanSat(mob))
			{
				return false;
			}
			if (PickMob.neSieuQuai && !ItemTime.isExistItem(4387) && mob.getTemplate().hp >= 3000)
			{
				if (mob.levelBoss != 0)
				{
					Mob mob2 = null;
					bool flag = false;
					for (int i = 0; i < GameScr.vMob.size(); i++)
					{
						mob2 = (Mob)GameScr.vMob.elementAt(i);
						if (mob2.countDie == 10 && (mob2.status == 0 || mob2.status == 1))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						return false;
					}
					mob.lastDie = mob2.lastDie;
				}
				else if (mob.countDie == 10 && (mob.status == 0 || mob.status == 1))
				{
					return false;
				}
			}
			return true;
		}

		private static bool FilterMobTanSat(Mob mob)
		{
			if ((PickMob.IdMobsTanSat.Count == 0 || PickMob.IdMobsTanSat.Contains(mob.mobId)) && (PickMob.TypeMobsTanSat.Count == 0 || PickMob.TypeMobsTanSat.Contains(mob.templateId)))
			{
				return !mob.isMobMe;
			}
			return false;
		}

		private static bool IsExcludedMassacreSkill(Skill skill)
		{
			if (skill == null || skill.template == null)
			{
				return true;
			}
			// Skill template IDs from the server: revive, spirit bomb, Makankosappo, self-destruct.
			return skill.template.id == 7 || skill.template.id == 10 || skill.template.id == 11 || skill.template.id == 14;
		}

		private static int NextMassacreSkillCursor(int current, int count)
		{
			if (count <= 0)
			{
				return 0;
			}
			return (current + 1) % count;
		}

		private static Skill GetSkillAttack()
		{
			Char player = Char.myCharz();
			int count = player.vSkill.size();
			if (count == 0)
			{
				return null;
			}
			int start = massacreSkillCursor % count;
			for (int offset = 0; offset < count; offset++)
			{
				int index = (start + offset) % count;
				Skill skill = (Skill)player.vSkill.elementAt(index);
				if (!IsExcludedMassacreSkill(skill) && skill.template.type >= 1 && skill.template.type <= 4 && CanUseSkill(skill))
				{
					return skill;
				}
			}
			return null;
		}

		private static void AdvanceMassacreSkillCursor(Skill usedSkill)
		{
			Char player = Char.myCharz();
			int count = player.vSkill.size();
			for (int index = 0; index < count; index++)
			{
				if (player.vSkill.elementAt(index) == usedSkill)
				{
					massacreSkillCursor = NextMassacreSkillCursor(index, count);
					return;
				}
			}
		}

		private static bool TryUseMassacreSkill(Skill skill, Mob targetMob, Char targetChar)
		{
			if (skill == null || IsExcludedMassacreSkill(skill) || !CanUseSkill(skill))
			{
				return false;
			}
			Char player = Char.myCharz();
			if (!skill.template.isBuffToPlayer() && !skill.template.isUseAlone() && targetMob == null && targetChar == null)
			{
				return false;
			}
			if (!EnsurePositionSent() || !GameScr.gI().TrySelectSkillForAutoCombat(skill))
			{
				return false;
			}
			bool used;
			if (skill.template.isBuffToPlayer())
			{
				MyVector targetPlayers = new MyVector();
				targetPlayers.addElement(player);
				used = Service.gI().TrySendPlayerAttack(new MyVector(), targetPlayers, 2);
			}
			else if (skill.template.isUseAlone())
			{
				player.useSkillNotFocus();
				used = true;
			}
			else
			{
				MyVector targetMobs = new MyVector();
				MyVector targetPlayers = new MyVector();
				if (targetMob != null)
				{
					targetMobs.addElement(targetMob);
				}
				if (targetChar != null)
				{
					targetPlayers.addElement(targetChar);
				}
				used = Service.gI().TrySendPlayerAttack(targetMobs, targetPlayers, (targetChar == null) ? 1 : 2);
			}
			if (used)
			{
				AdvanceMassacreSkillCursor(skill);
				ModFunc.GI().SetUsedSkill(skill);
			}
			return used;
		}

		private static Skill GetSkillAttack2()
		{
			Skill skill = null;
			Skill[] array = (GameCanvas.isTouch ? GameScr.onScreenSkill : GameScr.keySkill);
			foreach (Skill skill2 in array)
			{
				if (skill2 != null && skill2.template.id != 10 && skill2.template.id != 11 && skill2.template.id != 14 && skill2.template.id != 23 && skill2.template.id != 27 && skill2.template.id != 28 && !skill2.template.isBuffToPlayer() && !skill2.template.isSkillSpec() && IsSkillBetter(skill2, skill))
				{
					skill = skill2;
				}
			}
			return skill;
		}

		private static bool IsSkillBetter(Skill SkillBetter, Skill skill)
		{
			if (!CanUseSkill(SkillBetter))
			{
				return false;
			}
			if (skill == null)
			{
				return true;
			}
			if (!CanUseSkill(SkillBetter))
			{
				return false;
			}
			if (!CanUseSkill(skill))
			{
				return true;
			}
			bool flag = (SkillBetter.template.id == 17 && skill.template.id == 2) || (SkillBetter.template.id == 9 && skill.template.id == 0);
			return skill.coolDown < SkillBetter.coolDown || flag;
		}

        private static bool CanUseSkill(Skill skill)
        {
            if (skill == null || skill.template == null) return false;
            if (mSystem.currentTimeMillis() - skill.lastTimeUseThisSkill <= (long)skill.coolDown + 25L) return false;
            Char player = Char.myCharz();
            if (skill.template.id == 9 && player.cHP <= player.cHPFull / 10) return false;
            skill.paintCanNotUseSkill = false;
            return player.cMP >= GetManaUseSkill(skill);
        }

		private static long GetManaUseSkill(Skill skill)
		{
			if (skill.template.manaUseType == 2)
			{
				return 1L;
			}
			if (skill.template.manaUseType == 1)
			{
				return skill.manaUse * Char.myCharz().cMPFull / 100;
			}
			return skill.manaUse;
		}

		public static int GetYsd(int xsd)
		{
			Char @char = Char.myCharz();
			int num = TileMap.pxh;
			int result = -1;
			for (int i = 24; i < TileMap.pxh; i += 24)
			{
				if (TileMap.tileTypeAt(xsd, i, 2))
				{
					int num2 = Res.abs(i - @char.cy);
					if (num2 < num)
					{
						num = num2;
						result = i;
					}
				}
			}
			return result;
		}

		private static int[] GetPointYsdMax(int xStart, int xEnd)
		{
			int num = TileMap.pxh;
			int num2 = -1;
			if (xStart > xEnd)
			{
				for (int i = xEnd; i < xStart; i += 24)
				{
					int ysd = GetYsd(i);
					if (ysd < num)
					{
						num = ysd;
						num2 = i;
					}
				}
			}
			else
			{
				for (int num3 = xEnd; num3 > xStart; num3 -= 24)
				{
					int ysd2 = GetYsd(num3);
					if (ysd2 < num)
					{
						num = ysd2;
						num2 = num3;
					}
				}
			}
			return new int[2] { num2, num };
		}

		public static void Wait(int time)
		{
			IsWait = true;
			TimeStartWait = mSystem.currentTimeMillis();
			TimeWait = time;
		}

		public static bool IsWaiting()
		{
			if (IsWait && mSystem.currentTimeMillis() - TimeStartWait >= TimeWait)
			{
				IsWait = false;
			}
			return IsWait;
		}
	}
}
