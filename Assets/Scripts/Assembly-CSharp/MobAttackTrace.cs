using System;

namespace NRO.Combat
{
	/// <summary>Temporary diagnostics for finding where a visible mob punch stops in the send/ACK/hit path.</summary>
	public static class MobAttackTrace
	{
		public static Action<string> Sink;

		private static void Write(string message)
		{
			string line = "[MobAttackTrace] " + DateTime.Now.ToString("HH:mm:ss.fff") + " " + message;
			Action<string> sink = Sink;
			if (sink != null) sink(line);
			else UnityEngine.Debug.Log(line);
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

		public static void RecordSend(int lane, int sequence, byte skillId, byte mobId)
		{
			Write("SEND lane=" + lane + " seq=" + sequence + " skill=" + skillId + " mob=" + mobId);
		}

		public static void RecordResult(int lane, MobAttackResult result)
		{
			Write("RESULT lane=" + lane + " seq=" + result.Sequence + " mob=" + result.MobId
				+ " status=" + StatusName(result.Status)
				+ " retryMs=" + result.RetryAfterMs);
		}

		public static void RecordHit(int lane, int mobId, long hp, long damage)
		{
			Write("HIT lane=" + lane + " mob=" + mobId + " hp=" + hp + " damage=" + damage);
		}
	}
}
