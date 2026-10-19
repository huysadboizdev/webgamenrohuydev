using System;
using System.Collections.Generic;

namespace NRO.Combat
{
	public struct MobAttackResult
	{
		public int Sequence;
		public byte Status;
		public byte SkillId;
		public byte MobId;
		public short PlayerX;
		public short PlayerY;
		public short MobX;
		public short MobY;
		public int RetryAfterMs;
	}

	public static class MobAttackResultTracker
	{
		public const byte Rejected = 0;
		public const byte Accepted = 1;
		public const byte OutOfRange = 2;
		public const byte Cooldown = 3;
		public const byte Preparing = 4;

		private sealed class PendingAttack
		{
			public byte SkillId;
			public byte MobId;
			public long StartedAtTicks;
			public Action<MobAttackResult> Callback;
		}

		private sealed class LaneState
		{
			public readonly object Sync = new object();
			public readonly Dictionary<int, PendingAttack> Pending = new Dictionary<int, PendingAttack>();
			public readonly Dictionary<byte, long> RetryAt = new Dictionary<byte, long>();
			public readonly Dictionary<byte, int> LatestSequence = new Dictionary<byte, int>();
			public int NextSequence;
		}
		private static readonly LaneState[] Lanes = { new LaneState(), new LaneState(), new LaneState() };
		private static LaneState Lane(int lane) { return Lanes[lane >= 0 && lane < Lanes.Length ? lane : 0]; }
		private static void PurgeExpired(LaneState state)
		{
			long now = DateTime.UtcNow.Ticks;
			List<int> expired = null;
			foreach (KeyValuePair<int, PendingAttack> entry in state.Pending)
			{
				if (now - entry.Value.StartedAtTicks >= TimeSpan.TicksPerSecond * 8L)
				{
					if (expired == null) expired = new List<int>();
					expired.Add(entry.Key);
				}
			}
			if (expired != null) foreach (int sequence in expired) state.Pending.Remove(sequence);
		}

		public static bool HasPending
		{
			get { return HasPendingFor(0); }
		}
		public static bool HasPendingFor(int lane) { LaneState state = Lane(lane); lock (state.Sync) { PurgeExpired(state); return state.Pending.Count > 0; } }
		public static bool HasPendingSkillFor(int lane, byte skillId, byte mobId)
		{
			LaneState state = Lane(lane);
			lock (state.Sync)
			{
				PurgeExpired(state);
				foreach (PendingAttack pending in state.Pending.Values)
					if (pending.SkillId == skillId && pending.MobId == mobId) return true;
				return false;
			}
		}

		public static bool CanAttempt(byte skillId, long now)
		{
			return CanAttemptFor(0, skillId, now);
		}
		public static bool CanAttemptFor(int lane, byte skillId, long now)
		{
			LaneState state = Lane(lane);
			lock (state.Sync)
			{
				PurgeExpired(state);
				long retryAt;
				return !state.RetryAt.TryGetValue(skillId, out retryAt) || now >= retryAt;
			}
		}

		public static int Begin(byte skillId, byte mobId, Action<MobAttackResult> callback)
		{
			return BeginFor(0, skillId, mobId, callback);
		}
		public static int BeginFor(int lane, byte skillId, byte mobId, Action<MobAttackResult> callback)
		{
			LaneState state = Lane(lane);
			int sequence;
			lock (state.Sync)
			{
				PurgeExpired(state);
				sequence = ++state.NextSequence;
				if (sequence <= 0) sequence = state.NextSequence = 1;
				state.Pending.Add(sequence, new PendingAttack { SkillId = skillId, MobId = mobId, StartedAtTicks = DateTime.UtcNow.Ticks, Callback = callback });
				state.LatestSequence[skillId] = sequence;
			}
			return sequence;
		}

		public static bool Resolve(MobAttackResult result, long now)
		{
			return ResolveFor(0, result, now);
		}
		public static bool ResolveFor(int lane, MobAttackResult result, long now)
		{
			LaneState state = Lane(lane);
			Action<MobAttackResult> callback;
			lock (state.Sync)
			{
				PendingAttack pending;
				if (!state.Pending.TryGetValue(result.Sequence, out pending)) return false;
				state.Pending.Remove(result.Sequence);
				int latest;
				if (!state.LatestSequence.TryGetValue(pending.SkillId, out latest) || latest != result.Sequence) return false;
				result.SkillId = pending.SkillId;
				result.MobId = pending.MobId;
				callback = pending.Callback;
				// Successful hits keep their locally predicted cooldown. ACK travel time
				// must never restart that cooldown or hold up another ready punch.
				if (result.Status == Accepted || result.RetryAfterMs <= 0) state.RetryAt.Remove(pending.SkillId);
				else state.RetryAt[pending.SkillId] = now + Math.Min(result.RetryAfterMs, 5000);
			}
			if (callback != null) callback(result);
			return true;
		}

		public static void Cancel(int sequence)
		{
			CancelFor(0, sequence);
		}
		public static void CancelFor(int lane, int sequence) { LaneState state = Lane(lane); lock (state.Sync) state.Pending.Remove(sequence); }

		public static void Clear()
		{
			ClearFor(0);
		}
		public static void ClearFor(int lane)
		{
			LaneState state = Lane(lane);
			lock (state.Sync)
			{
				state.Pending.Clear();
				state.RetryAt.Clear();
				state.LatestSequence.Clear();
			}
		}
	}
}
