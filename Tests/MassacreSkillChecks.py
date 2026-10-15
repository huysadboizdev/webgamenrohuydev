"""Run small C# regressions against the real client helper methods."""
from pathlib import Path
import subprocess
import tempfile
import unittest

WEB = Path(__file__).resolve().parents[1]
APK = Path(r"C:\Users\Admin\Downloads\PRJ_2Tab_550K")
ROOTS = (WEB, APK)


def extract_method(source: str, signature: str) -> str:
    start = source.index(signature)
    opening = source.index("{", start) + 1
    depth, end = 1, opening
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


def run_csharp(methods: str, game: str) -> None:
    harness = r"""
using System;
using System.Collections.Generic;
public static class Math { public static int abs(int value) { return System.Math.Abs(value); } }
public class SkillInfoPaint { }
public class SkillTemplate {
    public sbyte id;
    public string name;
    public sbyte type;
    public bool isBuffToPlayer() { return type == 2; }
    public bool isUseAlone() { return type == 3; }
}
public class Skill { public SkillTemplate template; public bool ready = true; public int dx; public int dy; }
public class MyVector {
    readonly List<object> values = new List<object>();
    public int size() { return values.Count; }
    public object elementAt(int index) { return values[index]; }
    public void addElement(object value) { values.Add(value); }
}
public class Mob { }
public class Char {
    public MyVector vSkill = new MyVector();
    public int selfUses;
    public int indexSkill;
    public SkillInfoPaint[] animation;
    public static Char instance = new Char();
    public static Char myCharz() { return instance; }
    public void useSkillNotFocus() { selfUses++; }
    public SkillInfoPaint[] skillInfoPaint() { return animation; }
}
public class GameScr {
    public static GameScr instance = new GameScr();
    public static GameScr gI() { return instance; }
    public bool TrySelectSkillForAutoCombat(Skill skill) { return skill != null; }
}
public class Service {
    public static Service instance = new Service();
    public MyVector mobs, chars;
    public int type;
    public static Service gI() { return instance; }
    public bool TrySendPlayerAttack(MyVector mobTargets, MyVector charTargets, int attackType) {
        mobs = mobTargets; chars = charTargets; type = attackType; return true;
    }
}
public class ModFunc {
    public static ModFunc instance = new ModFunc();
    public static ModFunc GI() { return instance; }
    public void SetUsedSkill(Skill skill) { }
}
namespace Game1.Mod.XMAP {
public static class PickMob {
    public static bool tanSat;
    public static bool tsPlayer;
}
}
namespace Game2.Mod.XMAP {
public static class PickMob {
    public static bool tanSat;
    public static bool tsPlayer;
}
}
public class PickMobController {
    private static int massacreSkillCursor;
    METHODS
    private static bool CanUseSkill(Skill skill) { return skill != null && skill.ready; }
    private static bool EnsurePositionSent() { return true; }
    public static void Run() {
        foreach (sbyte id in new sbyte[] { 7, 10, 11, 14 })
            if (!IsExcludedMassacreSkill(new Skill { template = new SkillTemplate { id = id } }))
                throw new Exception("forbidden skill ID was selected: " + id);
        if (IsExcludedMassacreSkill(new Skill { template = new SkillTemplate { id = 23 } }))
            throw new Exception("an unlisted skill was excluded");
        if (NextMassacreSkillCursor(0, 3) != 1 || NextMassacreSkillCursor(2, 3) != 0)
            throw new Exception("skill cursor did not rotate");
        Skill shortRange = new Skill { dx = 40, dy = 25 };
        if (!IsWithinMassacreSkillRange(shortRange, 135, 210, 100, 200))
            throw new Exception("valid short-range target was rejected");
        if (IsWithinMassacreSkillRange(shortRange, 141, 210, 100, 200))
            throw new Exception("out-of-range target was attacked instead of approached");
        if (IsWithinMassacreSkillRange(shortRange, 120, 226, 100, 200))
            throw new Exception("target on another vertical level was attacked");
        if (GetMassacreApproachX(shortRange, 200, 100) != 180
            || GetMassacreApproachX(shortRange, 100, 200) != 120)
            throw new Exception("approach point did not preserve the skill's attack spacing");
        Char.instance.animation = new SkillInfoPaint[] { new SkillInfoPaint(), new SkillInfoPaint() };
        Char.instance.indexSkill = 0;
        if (CanStartMassacreSkill(Char.instance))
            throw new Exception("controller overlapped a skill before the hidden pose finished");
        Char.instance.indexSkill = 2;
        if (!CanStartMassacreSkill(Char.instance))
            throw new Exception("completed hidden pose kept the controller blocked");
        Char.instance = new Char();
        Char.instance.vSkill.addElement(new Skill { template = new SkillTemplate { id = 0, type = 1 } });
        foreach (sbyte blocked in new sbyte[] { 7, 10, 11, 14 })
            Char.instance.vSkill.addElement(new Skill { template = new SkillTemplate { id = blocked, type = 1 } });
        Char.instance.vSkill.addElement(new Skill { template = new SkillTemplate { id = 23, type = 1 } });
        Char.instance.vSkill.addElement(new Skill { template = new SkillTemplate { id = 19, type = 2 } });
        Char.instance.vSkill.addElement(new Skill { template = new SkillTemplate { id = 6, type = 3 } });
        Char.instance.vSkill.addElement(new Skill { template = new SkillTemplate { id = 24, type = 4 } });
        massacreSkillCursor = 0;
        sbyte[] expectedOrder = new sbyte[] { 0, 23, 19, 6, 24 };
        if (GetSkillAttack().template.id != 0 || GetSkillAttack().template.id != 0)
            throw new Exception("skill cursor advanced before a skill was successfully used");
        foreach (sbyte expected in expectedOrder) {
            Skill selected = GetSkillAttack();
            if (selected == null || selected.template.id != expected)
                throw new Exception("learned skills were not selected fairly; expected " + expected);
            AdvanceMassacreSkillCursor(selected);
        }
        Skill buff = new Skill { template = new SkillTemplate { id = 19, type = 2 } };
        if (!TryUseMassacreSkill(buff, null, null) || Service.instance.chars.size() != 1
            || !Object.ReferenceEquals(Service.instance.chars.elementAt(0), Char.instance))
            throw new Exception("buff was not sent to the local character");
        Skill self = new Skill { template = new SkillTemplate { id = 6, type = 3 } };
        if (!TryUseMassacreSkill(self, null, null) || Char.instance.selfUses != 1)
            throw new Exception("self-use skill was not activated without a target");
        Mob targetMob = new Mob();
        if (!TryUseMassacreSkill(new Skill { template = new SkillTemplate { id = 0, type = 1 } }, targetMob, null)
            || Service.instance.mobs.size() != 1 || !Object.ReferenceEquals(Service.instance.mobs.elementAt(0), targetMob))
            throw new Exception("attack skill was not sent to the selected mob");
        Char targetChar = new Char();
        if (!TryUseMassacreSkill(new Skill { template = new SkillTemplate { id = 24, type = 4 } }, null, targetChar)
            || Service.instance.chars.size() != 1 || !Object.ReferenceEquals(Service.instance.chars.elementAt(0), targetChar))
            throw new Exception("special target skill was not sent to the selected player");
        if (ShouldSuppressOwnSkillAnimation(false))
            throw new Exception("other characters must keep their animations");
        ACTIVE_NAMESPACE.PickMob.tanSat = true;
        if (!ShouldSuppressOwnSkillAnimation(true))
            throw new Exception("own animation should be hidden during massacre");
        ACTIVE_NAMESPACE.PickMob.tanSat = false;
        ACTIVE_NAMESPACE.PickMob.tsPlayer = true;
        if (!ShouldSuppressOwnSkillAnimation(true))
            throw new Exception("own animation should be hidden during player massacre");
        ACTIVE_NAMESPACE.PickMob.tsPlayer = false;
        if (ShouldSuppressOwnSkillAnimation(true))
            throw new Exception("own animation should return when massacre stops");
    }
}
""".replace("METHODS", methods).replace("ACTIVE_NAMESPACE", game + ".Mod.XMAP")
    with tempfile.TemporaryDirectory() as directory:
        cs = Path(directory) / "MassacreSkillChecks.cs"
        cs.write_text(harness, encoding="utf-8-sig")
        command = "Add-Type -Path '" + str(cs).replace("'", "''") + "'; [PickMobController]::Run()"
        result = subprocess.run(
            ["powershell", "-NoProfile", "-NonInteractive", "-Command", command],
            capture_output=True,
            text=True,
        )
        if result.returncode:
            raise AssertionError(result.stdout + result.stderr)


class MassacreSkillChecks(unittest.TestCase):
    def test_helpers_in_both_games_and_both_clients(self):
        for root in ROOTS:
            for game in ("Game1", "Game2"):
                with self.subTest(root=root.name, game=game):
                    source_root = root / "Assets/Scripts/Assembly-CSharp" / game
                    controller = (source_root / "Mod/XMAP/PickMobController.cs").read_text(encoding="utf-8-sig")
                    character = (source_root / "Char.cs").read_text(encoding="utf-8-sig")
                    self.assertIn("IsWithinMassacreSkillRange(skillAttack, target.getX(), target.getY()", controller)
                    self.assertIn("IsWithinMassacreSkillRange(skillAttack2, charFocus.cx, charFocus.cy", controller)
                    self.assertNotIn("Res.distance(target.xFirst, target.yFirst, @char.cx, @char.cy) > 48", controller)
                    methods = "\n".join(
                        extract_method(controller, signature)
                        for signature in (
                            "private static bool IsExcludedMassacreSkill(Skill skill)",
                            "private static int NextMassacreSkillCursor(int current, int count)",
                            "private static bool CanStartMassacreSkill(Char player)",
                            "private static bool IsWithinMassacreSkillRange(Skill skill, int targetX, int targetY, int playerX, int playerY)",
                            "private static int GetMassacreApproachX(Skill skill, int targetX, int playerX)",
                            "private static Skill GetSkillAttack()",
                            "private static void AdvanceMassacreSkillCursor(Skill usedSkill)",
                            "private static bool TryUseMassacreSkill(Skill skill, Mob targetMob, Char targetChar)",
                        )
                    )
                    methods += "\n" + extract_method(
                        character, "private static bool ShouldSuppressOwnSkillAnimation(bool isOwnCharacter)"
                    )
                    paint = extract_method(character, "public virtual void paint(mGraphics g)")
                    self.assertIn("if (hideOwnSkillAnimation || skillPaint == null", paint)
                    self.assertIn("if (!hideOwnSkillAnimation && arr != null)", paint)
                    self.assertIn("if (!hideOwnSkillAnimation && dart != null)", paint)
                    self.assertIn("paintEffect(g);", paint)
                    self.assertIn("if (hideOwnSkillAnimation && (skillPaint != null || isPaintNewSkill)) cf = 0;", paint)
                    skill_paint = extract_method(character, "public void paintCharWithSkill(mGraphics g)")
                    self.assertLess(
                        skill_paint.index("if (ShouldSuppressOwnSkillAnimation(me))"),
                        skill_paint.index("cf = array[indexSkill].status;"),
                    )
                    self.assertIn("indexSkill = array.Length;", skill_paint)
                    game_scr = (source_root / "GameScr.cs").read_text(encoding="utf-8-sig")
                    self.assertIn("Char.myCharz().paintCharWithSkill(g);", game_scr)
                    self.assertIn("paintSelectedSkill(g);", game_scr)
                    run_csharp(methods, game)


if __name__ == "__main__":
    unittest.main(verbosity=2)
