"""Check that lethal mob damage packets are included in ClientWeb combat traces."""
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]
ASSEMBLY = ROOT / "Assets" / "Scripts" / "Assembly-CSharp"


class CombatDeathTraceChecks(unittest.TestCase):
    def test_lethal_packet_is_recorded_for_both_account_lanes(self):
        diagnostics = (ASSEMBLY / "ClientWebCombatDiagnostics.cs").read_text(encoding="utf-8-sig")
        self.assertIn("RecordMobKill(", diagnostics)
        self.assertIn('Write("MOB_KILL lane="', diagnostics)
        self.assertIn('"KILL_MATCH"', diagnostics)
        self.assertIn("PendingAcks", diagnostics)
        self.assertIn("EarlyHits", diagnostics)

        for game, lane in (("Game1", 1), ("Game2", 2)):
            with self.subTest(game=game):
                controller = (ASSEMBLY / game / "Controller.cs").read_text(encoding="utf-8-sig")
                self.assertIn(f"ClientWebCombatDiagnostics.RecordMobKill({lane}, mob12.mobId, num196)", controller)


if __name__ == "__main__":
    unittest.main(verbosity=2)
