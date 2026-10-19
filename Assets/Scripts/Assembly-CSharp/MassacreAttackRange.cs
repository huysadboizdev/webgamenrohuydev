namespace NRO.Combat
{
    public static class MassacreAttackRange
    {
        public const int ServerMeleeMaxDistance = 100;
        public const int PositionSafetyMargin = 20;
        public const int SafeMeleeDistance = ServerMeleeMaxDistance - PositionSafetyMargin;

        public static bool RequiresServerMeleeRange(int skillId)
        {
            return skillId == 0 || skillId == 2 || skillId == 4 || skillId == 17;
        }

        public static bool IsWithinSkillRange(int skillId, long deltaX, long deltaY, int skillRangeX, int skillRangeY)
        {
            if (deltaX < 0 || deltaY < 0 || deltaX > skillRangeX || deltaY > skillRangeY)
                return false;

            if (!RequiresServerMeleeRange(skillId))
                return true;

            long safeDistance = SafeMeleeDistance;
            return deltaX * deltaX + deltaY * deltaY <= safeDistance * safeDistance;
        }

        public static bool IsWithinSafeRange(long deltaX, long deltaY, int skillRangeX, int skillRangeY)
        {
            return IsWithinSkillRange(0, deltaX, deltaY, skillRangeX, skillRangeY);
        }

        public static int GetApproachOffset(int skillId, int skillRangeX)
        {
            int desiredOffset = System.Math.Max(0, skillRangeX - PositionSafetyMargin);
            return RequiresServerMeleeRange(skillId)
                ? System.Math.Min(SafeMeleeDistance, desiredOffset)
                : desiredOffset;
        }
    }
}
