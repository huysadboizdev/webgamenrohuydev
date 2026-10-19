"""Regression checks for Tàn Sát eligibility of offensive mob skills."""
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
ASSEMBLY = ROOT / "Assets" / "Scripts" / "Assembly-CSharp"
CATALOG = ASSEMBLY / "MobAttackSkillCatalog.cs"


def extract_method(source: str, signature: str) -> str:
    start = source.index(signature)
    opening = source.index("{", start) + 1
    depth, end = 1, opening
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


class AllSkillsMobDamageChecks(unittest.TestCase):
    def test_tansat_uses_shared_offensive_skill_catalog_in_both_lanes(self):
        self.assertTrue(CATALOG.exists(), "mob attack eligibility must have one shared catalog")
        catalog = CATALOG.read_text(encoding="utf-8-sig")
        eligibility = extract_method(catalog, "public static bool IsOffensiveMobSkill(int skillId)")
        for skill_id in (0, 1, 2, 3, 4, 5, 9, 10, 11, 14, 17, 20, 24, 25):
            self.assertIn(f"case {skill_id}:", eligibility, f"offensive skill {skill_id} is missing from the catalog")
        for skill_id in (6, 7, 8, 12, 13, 18, 19, 21, 22, 23, 26):
            self.assertNotIn(f"case {skill_id}:", eligibility, f"non-damage skill {skill_id} entered the mob rotation")

        lane_methods = {}
        for game in ("Game1", "Game2"):
            with self.subTest(game=game):
                source = (ASSEMBLY / game / "Mod" / "XMAP" / "PickMobController.cs").read_text(
                    encoding="utf-8-sig"
                )
                selector = extract_method(source, "private static Skill GetSkillAttack(bool mobTarget)")
                use_skill = extract_method(source, "private static bool TryUseMassacreSkill(Skill skill, Mob targetMob, Char targetChar)")
                lane_methods[game] = (selector, use_skill)
                self.assertIn("MobAttackSkillCatalog.IsOffensiveMobSkill(skill.template.id)", selector)
                self.assertIn("MobAttackSkillCatalog.IsOffensiveMobSkill(skill.template.id)", use_skill)
                self.assertIn("TrySendSelfMobSkill(skill", use_skill)

                service = (ASSEMBLY / game / "Service.cs").read_text(encoding="utf-8-sig")
                self.assertIn("public bool TrySendSelfMobSkill(Skill skill", service)
                self.assertIn("writeByte(0xA7)", service)
                self.assertIn("MobAttackResultTracker.BeginFor(" + ("1" if game == "Game1" else "2"), service)
        normalize_lane = lambda source, lane: source.replace(f"For({lane},", "For(LANE,")
        for index in (0, 1):
            self.assertEqual(normalize_lane(lane_methods["Game1"][index], 1),
                             normalize_lane(lane_methods["Game2"][index], 2))

    def test_preparation_is_a_distinct_retryable_server_result(self):
        tracker = (ASSEMBLY / "MobAttackResultTracker.cs").read_text(encoding="utf-8-sig")
        diagnostics = (ASSEMBLY / "ClientWebCombatDiagnostics.cs").read_text(encoding="utf-8-sig")
        self.assertIn("public const byte Preparing = 4;", tracker)
        self.assertIn('case MobAttackResultTracker.Preparing: return "PREPARING";', diagnostics)
        self.assertIn("result.Status != MobAttackResultTracker.Preparing", diagnostics)
        server = (ROOT.parent.parent / "SRC/src/nro/models/services/SkillService.java").read_text(
            encoding="utf-8-sig"
        )
        self.assertIn("PREPARING(4)", server)
        self.assertIn("getMobCastPrepareRetryAfterMs", server)
        self.assertIn("Skill.TU_SAT", server)
        server_allowlist = extract_method(server, "private boolean isOffensiveMobSkill(int skillTemplateId)")
        self.assertNotIn("Skill.MA_PHONG_BA", server_allowlist,
                         "control-only Mafuba must not be reported as mob damage")
        self.assertIn("case 10: return 6500", diagnostics)
        self.assertIn("private static bool IsAreaMobSkill(byte skillId)", diagnostics)
        self.assertIn('"AREA_" + kind + "_UNATTRIBUTED lane="', diagnostics)
        self.assertIn("attribution=ambiguous", diagnostics)
        controller = (ROOT.parent.parent / "SRC/src/nro/models/server/Controller.java").read_text(
            encoding="utf-8-sig"
        )
        self.assertIn("attackMobSelfConfirmed", controller)
        self.assertIn("_msg.reader().mark(6)", controller)
        self.assertIn("_msg.reader().reset()", controller)


if __name__ == "__main__":
    unittest.main(verbosity=2)
