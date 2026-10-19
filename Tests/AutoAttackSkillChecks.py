"""Exercise the real ModFunc.AutoAttack source with lightweight C# stubs."""
from pathlib import Path
import re
import subprocess
import tempfile
import unittest


WEB = Path(__file__).resolve().parents[1]
ANDROID = Path(r"C:\Users\Admin\Downloads\PRJ_2Tab_550K")
ROOTS = (WEB, ANDROID)


def extract_method(source: str, signature: str) -> str:
    start = source.index(signature)
    opening = source.index("{", start)
    depth, end = 1, opening + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


def extract_case(source: str, method_signature: str, case_label: str) -> str:
    method = extract_method(source, method_signature)
    start = method.index(case_label)
    next_case = re.search(r"\n\s*case\s+", method[start + len(case_label):])
    end = start + len(case_label) + next_case.start() if next_case else method.rfind("}")
    return method[start:end].strip()


def run_csharp(methods: str, toggle_case: str, game: str) -> None:
    harness = r"""
using System;
using System.Collections.Generic;
public class SkillTemplate {
    public sbyte id;
    public sbyte type;
    public int manaUseType;
    public bool isBuffToPlayer() { return type == 2; }
    public bool isUseAlone() { return type == 3; }
}
public class Skill {
    public SkillTemplate template;
    public int dx = 100, dy = 100, skillId;
    public int manaUse;
    public bool paintCanNotUseSkill;
    public long lastTimeUseThisSkill;
    public int coolDown;
}
public class MovePoint {
    public int xEnd, yEnd;
    public MovePoint(int x, int y) { xEnd = x; yEnd = y; }
}
public class Command {
    public int action;
    public Command(object a, object b, int action, object d) { this.action = action; }
}
public class MobAttackResult { public int Status, RetryAfterMs; }
public static class MobAttackResultTracker {
    public const int Accepted = 1, OutOfRange = 2;
    public static bool CanAttemptFor(int game, byte skillId, long now) { return true; }
}
namespace NRO.Combat {
    public static class MassacreAttackRange {
        public static bool IsWithinSkillRange(int skillId, long dx, long dy, int rangeX, int rangeY) {
            return dx <= rangeX * 2L && dy <= rangeY * 2L;
        }
        public static int GetApproachOffset(int skillId, int rangeX) { return System.Math.Max(0, rangeX - 20); }
    }
}
public class MyVector {
    readonly List<object> values = new List<object>();
    public int size() { return values.Count; }
    public object elementAt(int index) { return values[index]; }
    public void addElement(object value) { values.Add(value); }
}
public class Mob {
    public int xFirst, yFirst, status = 2, currentX = int.MinValue, currentY = int.MinValue;
    public int getX() { return currentX == int.MinValue ? xFirst : currentX; }
    public int getY() { return currentY == int.MinValue ? yFirst : currentY; }
}
public class Char {
    public static bool isLoadingMap;
    public static Char instance = new Char();
    public Skill myskill;
    public Mob mobFocus;
    public Char charFocus;
    public object skillPaint, dart;
    public int cx, cy, cxSend, cySend, statusMe;
    public long cMP = 1000, cMPFull = 1000;
    public object currentMovePoint;
    public Command endMovePointCommand;
    public int cdir, cvx, cvy;
    public bool stone, meDead, canAttackPlayer = true;
    public int selfUses, chargeUses;
    public static Char myCharz() { return instance; }
    public bool isMeCanAttackOtherPlayer(Char target) { return target != null && canAttackPlayer; }
    public bool isStandAndCharge, isFlyAndCharge, isWaitMonkey, isUseSkillAfterCharge;
    public void useSkillNotFocus() { selfUses++; skillPaint = new object(); myskill.lastTimeUseThisSkill = mSystem.currentTimeMillis(); }
    public void sendUseChargeSkill() { chargeUses++; isStandAndCharge = true; }
}
public class GameScr {
    public static GameScr instance = new GameScr();
    public static GameScr gI() { return instance; }
    public static Info info1 = new Info();
    public bool canAttackMob = true;
    public int approachCalls, doFireCalls, fireDispatches;
    public Mob lastMobFocus;
    public Char lastCharFocus;
    public bool isMeCanAttackMob(Mob target) { return target != null && target.status != 0 && target.status != 1 && canAttackMob; }
    public void doFire(bool isFireByShortCut, bool skipWaypoint) {
        approachCalls++; doFireCalls++; fireDispatches++; lastMobFocus = Char.instance.mobFocus; lastCharFocus = Char.instance.charFocus;
    }
    public class Info { public void addInfo(string text, int type) { } }
}
public class Service {
    public static Service instance = new Service();
    public bool connected = true, acceptSend = true;
    public int sends;
    public MyVector mobs, chars;
    public int type;
    public static Service gI() { return instance; }
    public void sendPlayerAttack(MyVector mobTargets, MyVector charTargets, int attackType) {
        TrySendPlayerAttack(mobTargets, charTargets, attackType);
    }
    public bool TrySendPlayerAttack(MyVector mobTargets, MyVector charTargets, int attackType) {
        if (!connected || !acceptSend || mobTargets == null || charTargets == null || (mobTargets.size() == 0 && charTargets.size() == 0)) return false;
        sends++; mobs = mobTargets; chars = charTargets; type = attackType; return true;
    }
    public bool TryCharMove() { return true; }
    public bool TrySendPlayerAttack(MyVector mobTargets, MyVector charTargets, int attackType, Action<MobAttackResult> callback) {
        bool sent = TrySendPlayerAttack(mobTargets, charTargets, attackType);
        if (sent && callback != null) callback(new MobAttackResult { Status = MobAttackResultTracker.Accepted });
        return sent;
    }
}
public static class PickMob { public static bool tanSat, tsPlayer; }
public static class Res { public static int abs(int n) { return System.Math.Abs(n); } }
public static class mSystem {
    public static long now = 10000;
    public static long currentTimeMillis() { return now; }
}
public class ModFunc {
    public bool autoAttack;
    private long lastAutoAttack;
    public static ModFunc instance = new ModFunc();
    public static ModFunc GI() { return instance; }
    METHODS
    public void SetUsedSkill(Skill skill, int retryAfterMs) { SetUsedSkill(skill); }
    public void ToggleAkFromExtractedCase() { switch (42) { TOGGLE_CASE } }
    public static void Run() {
        ModFunc mod = new ModFunc();
        instance = mod;
        mod.ToggleAkFromExtractedCase();
        if (!mod.autoAttack) throw new Exception("/ak did not toggle on");
        mod.ToggleAkFromExtractedCase();
        if (mod.autoAttack) throw new Exception("/ak did not toggle off");

        Char.instance = NewPlayer(1, 1);
        Mob mob = new Mob { xFirst = 20, yFirst = 0 };
        Char.instance.mobFocus = mob;
        mod.AutoAttack();
        if (Service.instance.sends != 1 || Service.instance.mobs.size() != 1 || !Object.ReferenceEquals(Service.instance.mobs.elementAt(0), mob))
            throw new Exception("type-1 skill did not attack the focused mob");

        Reset(mod);
        Char.instance = NewPlayer(4, 24);
        Char.instance.charFocus = new Char { statusMe = 14 };
        mod.AutoAttack();
        if (GameScr.instance.doFireCalls != 1 || Service.instance.sends != 0
            || !Object.ReferenceEquals(GameScr.instance.lastCharFocus, Char.instance.charFocus))
            throw new Exception("type-4 special skill did not use the existing targeted fire path");

        Reset(mod);
        Char.instance = NewPlayer(4, 24);
        Char.instance.mobFocus = new Mob { xFirst = 20, yFirst = 0 };
        Char.instance.myskill.coolDown = 1200;
        mSystem.now += 1201;
        mod.AutoAttack();
        if (GameScr.instance.doFireCalls != 1 || Service.instance.sends != 0
            || !Object.ReferenceEquals(GameScr.instance.lastMobFocus, Char.instance.mobFocus))
            throw new Exception("type-4 special skill did not preserve the focused mob for the existing fire path");
        mod.AutoAttack();
        mSystem.now += 1199;
        mod.AutoAttack();
        if (GameScr.instance.fireDispatches != 1)
            throw new Exception("type-4 dispatch repeated before max(500ms, skill cooldown)");
        mSystem.now += 2;
        mod.AutoAttack();
        if (GameScr.instance.fireDispatches != 2)
            throw new Exception("type-4 dispatch did not resume after its pacing cooldown");

        Reset(mod);
        Char.instance = NewPlayer(4, 24);
        Char.instance.myskill.dx = 100;
        Char.instance.myskill.dy = 100;
        Char.instance.mobFocus = new Mob { xFirst = 500, yFirst = 0, currentX = 20, currentY = 0 };
        mod.AutoAttack();
        if (GameScr.instance.doFireCalls != 0 || Service.instance.sends != 0
            || Char.instance.currentMovePoint == null || Char.instance.endMovePointCommand == null)
            throw new Exception("out-of-range type-4 mob did not approach at skill spacing before dispatch");
        MovePoint approach = (MovePoint)Char.instance.currentMovePoint;
        if (approach.xEnd != 420 || approach.yEnd != 0)
            throw new Exception("type-4 approach did not use skill range and preserved xFirst/yFirst target coordinates");
        mSystem.now += 501;
        Char.instance.mobFocus.xFirst = 50;
        mod.AutoAttack();
        if (GameScr.instance.doFireCalls != 1 || Service.instance.sends != 0)
            throw new Exception("in-range type-4 mob did not dispatch through doFire after approaching");

        Reset(mod);
        Char.instance = NewPlayer(3, 6);
        mod.AutoAttack();
        if (Char.instance.selfUses != 1 || Service.instance.sends != 0)
            throw new Exception("type-3 offensive self-use skill did not use useSkillNotFocus");

        foreach (sbyte chargeId in new sbyte[] { 10, 11 }) {
            Reset(mod);
            Char.instance = NewPlayer(1, chargeId);
            Char.instance.mobFocus = new Mob { xFirst = 20, yFirst = 0 };
            mod.AutoAttack();
            if (Char.instance.chargeUses != 1 || Service.instance.sends != 0)
                throw new Exception("charge skill " + chargeId + " did not enter charge workflow exclusively");
            Char.instance.sendUseChargeSkill();
            mod.AutoAttack();
            if (Char.instance.chargeUses != 2)
                throw new Exception("active charge skill " + chargeId + " was restarted by AK");
        }

        foreach (SkillTemplate blocked in new SkillTemplate[] {
            new SkillTemplate { id = 19, type = 2 }, new SkillTemplate { id = 7, type = 3 },
            new SkillTemplate { id = 8, type = 3 }
        }) {
            Reset(mod);
            Char.instance = new Char { myskill = new Skill { template = blocked } };
            Char.instance.mobFocus = new Mob { xFirst = 1 };
            mod.AutoAttack();
            if (Service.instance.sends != 0 || Char.instance.selfUses != 0 || Char.instance.chargeUses != 0)
                throw new Exception("buff/support or revive skill was used (id " + blocked.id + ")");
        }

        Reset(mod);
        Char.instance = NewPlayer(1, 1);
        Char.instance.mobFocus = new Mob { xFirst = 20, yFirst = 0 };
        mod.AutoAttack();
        Char.instance.myskill = new Skill { template = new SkillTemplate { id = 23, type = 1 } };
        mSystem.now += 501;
        mod.AutoAttack();
        if (Service.instance.sends != 2 || Service.instance.mobs.elementAt(0) == null)
            throw new Exception("AK did not use the currently selected skill after a slot change");

        AssertBlocked(mod, NewPlayer(1, 1, dead: true), "dead");
        Char.isLoadingMap = true;
        AssertBlocked(mod, NewPlayer(1, 1), "loading map");
        Char.isLoadingMap = false;
        AssertBlocked(mod, NewPlayer(1, 1, stone: true), "stone");
        AssertBlocked(mod, NewPlayer(1, 1, status: 14), "stun");
        AssertBlocked(mod, NewPlayer(1, 1, status: 5), "stun");
        AssertBlocked(mod, NewPlayer(1, 1), "targetless");
        AssertBlocked(mod, NewPlayer(1, 1), "disconnected", connected: false);
        AssertBlocked(mod, NewPlayer(1, 1, mana: 0, manaUse: 20), "out of mana");
        AssertBlocked(mod, NewPlayer(1, 1, unavailable: true), "cooldown");
        PickMob.tanSat = true;
        AssertBlocked(mod, NewPlayer(1, 1), "Tàn sát");
        PickMob.tanSat = false;
        PickMob.tsPlayer = true;
        AssertBlocked(mod, NewPlayer(1, 1), "Tàn sát player");
        PickMob.tsPlayer = false;

        Reset(mod);
        Char.instance = NewPlayer(1, 1);
        Char.instance.mobFocus = new Mob { xFirst = 20, yFirst = 0 };
        mod.AutoAttack();
        mod.AutoAttack();
        if (Service.instance.sends != 1)
            throw new Exception("accepted skill was resent before cooldown became ready");
        if (!Char.instance.myskill.paintCanNotUseSkill)
            throw new Exception("accepted action did not update selected skill cooldown");

        Reset(mod);
        Char.instance = NewPlayer(1, 1);
        Char.instance.mobFocus = new Mob { xFirst = 20, yFirst = 0 };
        Service.instance.acceptSend = false;
        mod.AutoAttack();
        if (Char.instance.myskill.paintCanNotUseSkill)
            throw new Exception("failed attack send incorrectly marked skill as used");

        Reset(mod);
        Char.instance = NewPlayer(1, 1);
        Char.instance.mobFocus = new Mob { xFirst = 20, yFirst = 0, currentX = 500, currentY = 300 };
        mod.AutoAttack();
        if (Service.instance.sends != 1) throw new Exception("in-range target was not attacked");
        Reset(mod);
        Char.instance = NewPlayer(1, 1);
        Char.instance.mobFocus = new Mob { xFirst = 20, yFirst = 300 };
        mod.AutoAttack();
        if (Service.instance.sends != 0) throw new Exception("target outside vertical range was attacked");
        Reset(mod);
        Char.instance = NewPlayer(1, 1);
        Char.instance.mobFocus = new Mob { xFirst = 500, yFirst = 0 };
        mod.AutoAttack();
        if (Service.instance.sends != 0 || GameScr.instance.approachCalls == 0)
            throw new Exception("out-of-range target was not approached at skill spacing");
        Reset(mod);
        Char.instance = NewPlayer(1, 1);
        Char.instance.myskill.dx = 300;
        Char.instance.mobFocus = new Mob { xFirst = 250, yFirst = 0 };
        mod.AutoAttack();
        if (Service.instance.sends != 1 || GameScr.instance.approachCalls != 0)
            throw new Exception("ranged skill was unnecessarily forced into melee approach");
    }
    private static Char NewPlayer(sbyte type, sbyte id, bool dead = false, bool stone = false, int status = 0, bool unavailable = false, long mana = 1000, int manaUse = 0) {
        return new Char { myskill = new Skill { template = new SkillTemplate { type = type, id = id }, paintCanNotUseSkill = unavailable, manaUse = manaUse }, meDead = dead, stone = stone, statusMe = status, cMP = mana };
    }
    private static void Reset(ModFunc mod) {
        Service.instance = new Service(); Char.isLoadingMap = false; PickMob.tanSat = false; PickMob.tsPlayer = false;
        GameScr.instance = new GameScr();
        mSystem.now += 1000;
    }
    private static void AssertBlocked(ModFunc mod, Char player, string gate, bool connected = true) {
        Reset(mod); Char.instance = player; Service.instance.connected = connected;
        if (gate == "loading map") Char.isLoadingMap = true;
        if (gate == "Tàn sát") PickMob.tanSat = true;
        if (gate == "Tàn sát player") PickMob.tsPlayer = true;
        if (gate != "targetless") player.mobFocus = new Mob { xFirst = 10, yFirst = 0 };
        mod.AutoAttack();
        if (Service.instance.sends != 0) throw new Exception("AK sent while " + gate);
    }
}
""".replace("METHODS", methods).replace("TOGGLE_CASE", toggle_case)
    with tempfile.TemporaryDirectory() as directory:
        cs = Path(directory) / "AutoAttackSkillChecks.cs"
        cs.write_text(harness, encoding="utf-8-sig")
        command = "Add-Type -Path '" + str(cs).replace("'", "''") + "'; [ModFunc]::Run()"
        result = subprocess.run(
            ["powershell", "-NoProfile", "-NonInteractive", "-Command", command],
            capture_output=True,
            text=True,
        )
        if result.returncode:
            raise AssertionError(result.stdout + result.stderr)


class AutoAttackSkillChecks(unittest.TestCase):
    def test_chat_ak_dispatch_in_both_games_and_client_roots(self):
        for root in ROOTS:
            for game in ("Game1", "Game2"):
                with self.subTest(root=root.name, game=game):
                    source_path = root / "Assets/Scripts/Assembly-CSharp" / game / "ModFunc.cs"
                    source = source_path.read_text(encoding="utf-8-sig")
                    self.assertRegex(source, r'case\s+"ak"\s*:\s*perform\(42,\s*null\);\s*return true;')
                    toggle_case = extract_case(source, "public void perform(int idAction, object p)", "case 42:")
                    self.assertIn("autoAttack = !autoAttack;", toggle_case)
                    self.assertNotIn("TrySelectSkillForAutoCombat", toggle_case)

                    methods = "\n".join(
                        extract_method(source, signature)
                        for signature in (
                            "public void AttackMob(Mob mob)",
                            "private void AttackChar()",
                            "public void AutoAttack()",
                            "public void SetUsedSkill(Skill skill)",
                        )
                    )
                    run_csharp(methods, toggle_case, game)


if __name__ == "__main__":
    unittest.main(verbosity=2)
