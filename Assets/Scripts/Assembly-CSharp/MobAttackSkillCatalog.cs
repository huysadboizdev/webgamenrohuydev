namespace NRO.Combat
{
	/// <summary>
	/// Skill templates whose server execution applies HP damage to mobs.
	/// Type alone is insufficient: it includes non-damaging self-use skills.
	/// </summary>
	public static class MobAttackSkillCatalog
	{
		public static bool IsOffensiveMobSkill(int skillId)
		{
			switch (skillId)
			{
				case 0:  // Dragon punch
				case 1:  // Kamejoko
				case 2:  // Demon punch
				case 3:  // Masenko
				case 4:  // Galick punch
				case 5:  // Antomic
				case 9:  // Kaioken attack
				case 10: // Spirit bomb
				case 11: // Makankosappo
				case 14: // Self-destruct
				case 17: // Continuous punch
				case 20: // Instant transmission attack
				case 24: // Super Kamejoko
				case 25: // Continuous blast
					return true;
				default:
					return false;
			}
		}
	}
}
