using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace NRO.Combat
{
	/// <summary>Bounded in-game trace for manual mob attacks and instant transmission.</summary>
	public static class ClientWebCombatDiagnostics
	{
#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")] private static extern void NroCombatLogAppend(string line);
#endif

		private sealed class PendingDamage
		{
			public int Lane;
			public int Sequence;
			public byte SkillId;
			public int MobId;
			public DateTime AcceptedAt;
		}

		private sealed class PendingAreaCast
		{
			public int Lane;
			public int Sequence;
			public byte SkillId;
			public DateTime AcceptedAt;
			public int HitPackets;
		}

		private sealed class PendingAck
		{
			public int Lane;
			public int Sequence;
			public int MobId;
			public DateTime SentAt;
		}

		private sealed class PendingHit
		{
			public int Lane;
			public int MobId;
			public long Hp;
			public long Damage;
			public DateTime HitAt;
			public bool IsKill;
		}

		private sealed class PendingTeleport
		{
			public DateTime StartedAt;
			public bool TimedOut;
		}

		private const int MaxLines = 160;
		private const int MaxRememberedLateAcks = 256;
		private const int MaxEarlyHits = 128;
		private static readonly object Sync = new object();
		private static readonly List<string> Lines = new List<string>();
		private static readonly List<PendingAck> PendingAcks = new List<PendingAck>();
		private static readonly List<PendingDamage> AcceptedAttacks = new List<PendingDamage>();
		private static readonly List<PendingAreaCast> AcceptedAreaCasts = new List<PendingAreaCast>();
		private static readonly List<PendingHit> EarlyHits = new List<PendingHit>();
		private static readonly Dictionary<string, DateTime> TimedOutAcks = new Dictionary<string, DateTime>();
		private static readonly Queue<string> TimedOutAckOrder = new Queue<string>();
		private static readonly PendingTeleport[] Teleports = { null, null, null };
		private static bool panelOpen;
		private static Vector2 scroll;
		private static int sentCount;
		private static int attackAttemptCount;
		private static int attackBlockedCount;
		private static int nextAttackAttemptId;
		private static int noAckCount;
		private static int acceptedCount;
		private static int rejectedCount;
		private static int noDamageCount;
		private static int damageCount;
		private static int zeroDamageCount;
		private static int hitPackets;
		private static int killPackets;
		private static int killCount;
		private static int teleportStarts;
		private static int teleportConfirms;

		public static int RecordAttackAttempt(int lane, int type, int mobCount, int charCount, int mobId, long mobHp)
		{
			int attemptId;
			lock (Sync)
			{
				attemptId = ++nextAttackAttemptId;
				attackAttemptCount++;
			}
			Write("ATTACK_ATTEMPT id=" + attemptId + " lane=" + lane + " type=" + type
				+ " mobs=" + mobCount + " chars=" + charCount + " mob=" + mobId + " hp=" + mobHp);
			return attemptId;
		}

		public static void RecordAttackBlocked(int lane, int attemptId, string reason)
		{
			lock (Sync) attackBlockedCount++;
			Write("ATTACK_BLOCKED id=" + attemptId + " lane=" + lane + " reason=" + reason);
		}

		public static void RecordAttackPacketSent(int lane, int attemptId, int command, int mobId)
		{
			Write("ATTACK_PACKET_SENT id=" + attemptId + " lane=" + lane + " command=" + command + " mob=" + mobId);
		}

		private static void RememberTimedOutAck(PendingAck pending)
		{
			string key = pending.Lane + ":" + pending.Sequence;
			if (!TimedOutAcks.ContainsKey(key)) TimedOutAckOrder.Enqueue(key);
			TimedOutAcks[key] = pending.SentAt;
			while (TimedOutAckOrder.Count > MaxRememberedLateAcks) TimedOutAcks.Remove(TimedOutAckOrder.Dequeue());
		}

		private static void Write(string message)
		{
			string line = DateTime.Now.ToString("HH:mm:ss.fff") + " " + message;
			lock (Sync)
			{
				Lines.Add(line);
				if (Lines.Count > MaxLines) Lines.RemoveAt(0);
			}
#if UNITY_WEBGL && !UNITY_EDITOR
			NroCombatLogAppend(line);
#endif
		}

		private static int SafeLane(int lane)
		{
			return lane >= 1 && lane <= 2 ? lane : 1;
		}

		private static void ExpireAcceptedAttacks(DateTime now)
		{
			for (int i = PendingAcks.Count - 1; i >= 0; i--)
			{
				PendingAck pending = PendingAcks[i];
				if ((now - pending.SentAt).TotalMilliseconds < 2000) continue;
				PendingAcks.RemoveAt(i);
				RememberTimedOutAck(pending);
				noAckCount++;
				noDamageCount++;
				Write("NO_ACK lane=" + pending.Lane + " seq=" + pending.Sequence + " mob=" + pending.MobId + " windowMs=2000");
			}
			for (int i = AcceptedAttacks.Count - 1; i >= 0; i--)
			{
				PendingDamage pending = AcceptedAttacks[i];
				int windowMs = DamageResolutionWindowMs(pending.SkillId);
				if ((now - pending.AcceptedAt).TotalMilliseconds < windowMs) continue;
				AcceptedAttacks.RemoveAt(i);
				noDamageCount++;
				Write("NO_DAMAGE timeout lane=" + pending.Lane + " seq=" + pending.Sequence + " skill=" + pending.SkillId + " mob=" + pending.MobId + " windowMs=" + windowMs);
			}
			for (int i = AcceptedAreaCasts.Count - 1; i >= 0; i--)
			{
				PendingAreaCast pending = AcceptedAreaCasts[i];
				int windowMs = DamageResolutionWindowMs(pending.SkillId);
				if ((now - pending.AcceptedAt).TotalMilliseconds < windowMs) continue;
				AcceptedAreaCasts.RemoveAt(i);
				Write("AREA_WINDOW_END lane=" + pending.Lane + " seq=" + pending.Sequence + " skill=" + pending.SkillId
					+ " observedMobPackets=" + pending.HitPackets + " windowMs=" + windowMs + " attribution=ambiguous");
			}
			for (int i = EarlyHits.Count - 1; i >= 0; i--)
			{
				PendingHit hit = EarlyHits[i];
				if ((now - hit.HitAt).TotalMilliseconds < 2000) continue;
				EarlyHits.RemoveAt(i);
				Write("DAMAGE_UNMATCHED timeout lane=" + hit.Lane + " kind=" + (hit.IsKill ? "KILL" : "HIT")
					+ " mob=" + hit.MobId + " hp=" + hit.Hp + " damage=" + hit.Damage);
			}
			for (int lane = 1; lane <= 2; lane++)
			{
				PendingTeleport pending = Teleports[lane];
				if (pending != null && !pending.TimedOut && (now - pending.StartedAt).TotalSeconds >= 5)
				{
					pending.TimedOut = true;
					Write("TELEPORT_NO_CONFIRM lane=" + lane + " elapsedMs=" + (int)(now - pending.StartedAt).TotalMilliseconds);
				}
			}
		}

		private static int DamageResolutionWindowMs(byte skillId)
		{
			switch (skillId)
			{
				case 10: return 6500; // Spirit bomb charge and impact window.
				case 11: return 5000; // Makankosappo charge and impact window.
				case 14: return 4000; // Self-destruct preparation and explosion.
				case 24:
				case 25:
				default: return 2000;
			}
		}

		private static bool IsAreaMobSkill(byte skillId)
		{
			return skillId == 10 || skillId == 11 || skillId == 14 || skillId == 24 || skillId == 25;
		}

		public static string StatusName(byte status)
		{
			switch (status)
			{
				case MobAttackResultTracker.Accepted: return "ACCEPTED";
				case MobAttackResultTracker.OutOfRange: return "OUT_OF_RANGE";
				case MobAttackResultTracker.Cooldown: return "COOLDOWN";
				case MobAttackResultTracker.Preparing: return "PREPARING";
				default: return "REJECTED";
			}
		}

		public static void RecordSend(int lane, int attemptId, int sequence, byte skillId, byte mobId)
		{
			lock (Sync)
			{
				DateTime now = DateTime.UtcNow;
				ExpireAcceptedAttacks(now);
				sentCount++;
				PendingAcks.Add(new PendingAck { Lane = lane, Sequence = sequence, MobId = mobId, SentAt = now });
			}
		Write("SEND attempt=" + attemptId + " lane=" + lane + " seq=" + sequence + " skill=" + skillId + " mob=" + mobId);
		}

		public static void RecordResult(int lane, MobAttackResult result)
		{
			lock (Sync)
			{
				DateTime now = DateTime.UtcNow;
				ExpireAcceptedAttacks(now);
				string ackKey = lane + ":" + result.Sequence;
				DateTime sentAt;
				bool lateAck = TimedOutAcks.TryGetValue(ackKey, out sentAt);
				if (lateAck) TimedOutAcks.Remove(ackKey);
				else sentAt = DateTime.MinValue;
				for (int i = PendingAcks.Count - 1; i >= 0; i--)
					if (PendingAcks[i].Lane == lane && PendingAcks[i].Sequence == result.Sequence)
					{
						sentAt = PendingAcks[i].SentAt;
						PendingAcks.RemoveAt(i);
						break;
					}
				if (result.Status == MobAttackResultTracker.Accepted)
				{
					acceptedCount++;
					int earlyHit = -1;
					for (int i = 0; i < EarlyHits.Count; i++)
					{
						PendingHit hit = EarlyHits[i];
						if (hit.Lane == lane && hit.MobId == result.MobId
							&& (sentAt == DateTime.MinValue || hit.HitAt >= sentAt) && hit.HitAt <= now)
						{
							earlyHit = i;
							break;
						}
					}
					if (earlyHit >= 0)
					{
						if (lateAck && noDamageCount > 0) noDamageCount--;
						if (IsAreaMobSkill(result.SkillId))
						{
							AcceptedAreaCasts.Add(new PendingAreaCast { Lane = lane, Sequence = result.Sequence,
								SkillId = result.SkillId, AcceptedAt = now, HitPackets = 1 });
							Write("AREA_CAST_ACCEPTED lane=" + lane + " seq=" + result.Sequence + " skill=" + result.SkillId + " attribution=ambiguous");
						}
						RecordMatchedDamage(lane, result.Sequence, EarlyHits[earlyHit]);
					}
					else if (!lateAck && (result.MobId == 255 || IsAreaMobSkill(result.SkillId)))
					{
						AcceptedAreaCasts.Add(new PendingAreaCast { Lane = lane, Sequence = result.Sequence, SkillId = result.SkillId, AcceptedAt = now });
						Write("AREA_CAST_ACCEPTED lane=" + lane + " seq=" + result.Sequence + " skill=" + result.SkillId + " attribution=ambiguous");
					}
					else if (!lateAck) AcceptedAttacks.Add(new PendingDamage { Lane = lane, Sequence = result.Sequence, SkillId = result.SkillId, MobId = result.MobId, AcceptedAt = now });
					if (earlyHit >= 0) EarlyHits.RemoveAt(earlyHit);
				}
				else if (result.Status != MobAttackResultTracker.Preparing) rejectedCount++;
				if (lateAck) Write("RESULT_LATE lane=" + lane + " seq=" + result.Sequence + " status=" + StatusName(result.Status));
			}
			Write("RESULT lane=" + lane + " seq=" + result.Sequence + " mob=" + result.MobId
				+ " status=" + StatusName(result.Status)
				+ " retryMs=" + result.RetryAfterMs);
		}

		public static void RecordHit(int lane, int mobId, long hp, long damage)
		{
			lock (Sync)
			{
				DateTime now = DateTime.UtcNow;
				ExpireAcceptedAttacks(now);
				hitPackets++;
				int match = -1;
				for (int i = 0; i < AcceptedAttacks.Count; i++)
				{
					PendingDamage pending = AcceptedAttacks[i];
					if (pending.Lane == lane && pending.MobId == mobId) { match = i; break; }
				}
				if (match >= 0)
				{
					PendingDamage pending = AcceptedAttacks[match];
					AcceptedAttacks.RemoveAt(match);
					RecordMatchedDamage(lane, pending.Sequence, new PendingHit { Lane = lane, MobId = mobId, Hp = hp, Damage = damage, HitAt = now });
				}
				else
				{
					RecordAreaPacket(lane, mobId, "HIT", now);
					bool awaitingResult = false;
					for (int i = 0; i < PendingAcks.Count; i++)
						if (PendingAcks[i].Lane == lane && PendingAcks[i].MobId == mobId) { awaitingResult = true; break; }
					if (awaitingResult)
					{
						EarlyHits.Add(new PendingHit { Lane = lane, MobId = mobId, Hp = hp, Damage = damage, HitAt = now });
						if (EarlyHits.Count > MaxEarlyHits) EarlyHits.RemoveAt(0);
					}
				}
			}
			Write("HIT lane=" + lane + " mob=" + mobId + " hp=" + hp + " damage=" + damage);
		}

		public static void RecordMobKill(int lane, int mobId, long damage)
		{
			lock (Sync)
			{
				DateTime now = DateTime.UtcNow;
				ExpireAcceptedAttacks(now);
				killPackets++;
				int match = -1;
				for (int i = 0; i < AcceptedAttacks.Count; i++)
				{
					PendingDamage pending = AcceptedAttacks[i];
					if (pending.Lane == lane && pending.MobId == mobId) { match = i; break; }
				}
				if (match >= 0)
				{
					PendingDamage pending = AcceptedAttacks[match];
					AcceptedAttacks.RemoveAt(match);
					RecordMatchedDamage(lane, pending.Sequence, new PendingHit
					{
						Lane = lane, MobId = mobId, Hp = 0, Damage = damage, HitAt = now, IsKill = true
					});
				}
				else
				{
					RecordAreaPacket(lane, mobId, "KILL", now);
					bool awaitingResult = false;
					for (int i = 0; i < PendingAcks.Count; i++)
						if (PendingAcks[i].Lane == lane && PendingAcks[i].MobId == mobId) { awaitingResult = true; break; }
					if (awaitingResult)
					{
						EarlyHits.Add(new PendingHit
						{
							Lane = lane, MobId = mobId, Hp = 0, Damage = damage, HitAt = now, IsKill = true
						});
						if (EarlyHits.Count > MaxEarlyHits) EarlyHits.RemoveAt(0);
					}
				}
			}
			Write("MOB_KILL lane=" + lane + " mob=" + mobId + " damage=" + damage);
		}

		private static void RecordAreaPacket(int lane, int mobId, string kind, DateTime now)
		{
			int active = 0;
			for (int i = 0; i < AcceptedAreaCasts.Count; i++)
			{
				PendingAreaCast pending = AcceptedAreaCasts[i];
				if (pending.Lane != lane) continue;
				pending.HitPackets++;
				active++;
				Write("AREA_" + kind + "_UNATTRIBUTED lane=" + lane + " seq=" + pending.Sequence
					+ " skill=" + pending.SkillId + " mob=" + mobId + " windowMs=" + DamageResolutionWindowMs(pending.SkillId));
			}
			if (active > 1) Write("AREA_HIT_AMBIGUOUS lane=" + lane + " mob=" + mobId + " activeCasts=" + active);
		}

		private static void RecordMatchedDamage(int lane, int sequence, PendingHit hit)
		{
			if (hit.Damage > 0) damageCount++;
			else { zeroDamageCount++; noDamageCount++; }
			if (hit.IsKill) killCount++;
			Write((hit.IsKill ? "KILL_MATCH" : "HIT_MATCH") + " lane=" + lane + " seq=" + sequence
				+ " mob=" + hit.MobId + " hp=" + hit.Hp + " damage=" + hit.Damage);
		}

		public static void RecordTeleportStart(int lane, int skillId, int x, int y, int targetMob)
		{
			lane = SafeLane(lane);
			lock (Sync)
			{
				ExpireAcceptedAttacks(DateTime.UtcNow);
				teleportStarts++;
				Teleports[lane] = new PendingTeleport { StartedAt = DateTime.UtcNow };
			}
			Write("TELEPORT_START lane=" + lane + " skill=" + skillId + " from=" + x + "," + y + " targetMob=" + targetMob);
		}

		public static void RecordTeleportConfirm(int lane, int x, int y, int type)
		{
			lane = SafeLane(lane);
			bool hadPendingStart;
			lock (Sync)
			{
				ExpireAcceptedAttacks(DateTime.UtcNow);
				hadPendingStart = Teleports[lane] != null;
				if (hadPendingStart) teleportConfirms++;
				Teleports[lane] = null;
			}
			Write((hadPendingStart ? "TELEPORT_CONFIRM" : "POSITION_123") + " lane=" + lane + " type=" + type + " pos=" + x + "," + y);
		}

		public static string GetReport()
		{
			lock (Sync)
			{
				ExpireAcceptedAttacks(DateTime.UtcNow);
				StringBuilder report = new StringBuilder();
				report.AppendLine("ClientWeb combat trace (all counts since page load)");
				report.AppendLine("Manual attack: attempts=" + attackAttemptCount + " blocked=" + attackBlockedCount
					+ " sent=" + sentCount + " accepted=" + acceptedCount + " rejected=" + rejectedCount
					+ " damage=" + damageCount + " kills=" + killCount + " noDamage=" + noDamageCount + " zeroDamagePackets=" + zeroDamageCount
					+ " noAck=" + noAckCount + " hitPackets=" + hitPackets + " killPackets=" + killPackets
					+ " pendingAck=" + PendingAcks.Count + " pendingAccepted=" + AcceptedAttacks.Count
					+ " pendingArea=" + AcceptedAreaCasts.Count);
				report.AppendLine("Dich Chuyen Tuc Thoi: starts=" + teleportStarts + " confirms=" + teleportConfirms);
				report.AppendLine("NoDamage = no ACK within 2000ms, accepted single-target attack without a matching mob hit/kill within its skill window, or a zero-damage packet. Area hits are logged without exact attribution.");
				report.AppendLine("Recent events (max " + MaxLines + "): ");
				for (int i = 0; i < Lines.Count; i++) report.AppendLine(Lines[i]);
				return report.ToString();
			}
		}

		public static void TogglePanel() { panelOpen = !panelOpen; }

		public static void DrawPanel()
		{
			if (!panelOpen)
			{
				if (GUI.Button(new Rect(Screen.width - 112f, 12f, 100f, 36f), "LOG / F8"))
					panelOpen = true;
				if (!panelOpen) return;
			}
			string report = GetReport();
			float width = Mathf.Min(Screen.width - 24f, 900f);
			float height = Mathf.Min(Screen.height - 24f, 640f);
			Rect area = new Rect(12f, 12f, width, height);
			GUILayout.BeginArea(area, "Combat log | F8 dong/mo", GUI.skin.window);
			GUILayout.BeginHorizontal();
			GUILayout.Label("Log danh tay va Dich Chuyen Tuc Thoi");
			if (GUILayout.Button("Copy log", GUILayout.Width(100f)))
			{
				try { GUIUtility.systemCopyBuffer = report; }
				catch (Exception ex) { Debug.LogWarning("Combat log clipboard unavailable: " + ex.GetType().Name); }
			}
			if (GUILayout.Button("Dong", GUILayout.Width(70f))) panelOpen = false;
			GUILayout.EndHorizontal();
			scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			GUILayout.TextArea(report, GUILayout.ExpandHeight(true));
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}
	}
}

