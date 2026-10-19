"""Regression checks for ClientWeb's no-damage classification on lethal hits."""
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
ASSEMBLY = ROOT / "Assets" / "Scripts" / "Assembly-CSharp"


class CombatNoDamageRecoveryChecks(unittest.TestCase):
    def test_live_mob_kill_packet_is_not_discarded(self):
        for game, lane in (("Game1", 1), ("Game2", 2)):
            with self.subTest(game=game):
                controller = (ASSEMBLY / game / "Controller.cs").read_text(encoding="utf-8-sig")
                kill_case = controller.split("case -12:", 1)[1].split("case -11:", 1)[0]
                self.assertRegex(
                    kill_case,
                    re.compile(r"if\s*\(\s*mob12\s*==\s*null\s*\|\|\s*mob12\.status\s*==\s*1\s*\)"),
                    "ignore duplicate kill packets for an already-dying mob, but handle a live status-0 mob",
                )
                self.assertNotIn("mob12.status == 0", kill_case)
                self.assertIn(f"ClientWebCombatDiagnostics.RecordMobKill({lane}, mob12.mobId, num196)", kill_case)


if __name__ == "__main__":
    unittest.main(verbosity=2)
