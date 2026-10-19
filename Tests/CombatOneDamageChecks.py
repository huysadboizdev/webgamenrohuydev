"""Regression check for one-damage mob hits in both WebGL account lanes."""
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
ASSEMBLY = ROOT / "Assets" / "Scripts" / "Assembly-CSharp"


class CombatOneDamageChecks(unittest.TestCase):
    def test_positive_damage_including_one_is_animated_and_displayed(self):
        for game in ("Game1", "Game2"):
            with self.subTest(game=game):
                source = (ASSEMBLY / game / "Controller.cs").read_text(encoding="utf-8-sig")
                start = source.index("case -9:")
                end = source.index("case -8:", start)
                handler = source[start:end]

                self.assertNotRegex(handler, r"if\s*\(\s*num204\s*!=\s*1\s*\)")
                self.assertRegex(handler, r"if\s*\(\s*num204\s*>\s*0\s*\)\s*\{\s*mob14\.setInjure\(\);\s*\}")
                self.assertRegex(
                    handler,
                    r"else if\s*\(\s*num204\s*>\s*0\s*\)\s*\{\s*GameScr\.startFlyText\(\"-\"\s*\+\s*num204",
                )
                self.assertIn("num204 == 0L", handler, "zero-damage miss feedback must stay intact")


if __name__ == "__main__":
    unittest.main(verbosity=2)
