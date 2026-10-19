"""Check native/Web parity for the per-tab server-acknowledged mob attack path."""
from pathlib import Path
import subprocess
import tempfile
import unittest


WEB = Path(__file__).resolve().parents[1]
NATIVE = Path(r"C:\Users\Admin\Downloads\PRJ_2Tab_550K")
ROOTS = (("Web", WEB), ("Native", NATIVE))


def read(root: Path, relative: str) -> str:
    return (root / "Assets/Scripts/Assembly-CSharp" / relative).read_text(encoding="utf-8-sig")


class CombatAttackParityChecks(unittest.TestCase):
    def test_shared_result_tracker_matches_native(self):
        for name in ("MobAttackResultTracker.cs", "MobAttackTrace.cs"):
            with self.subTest(file=name):
                web_source = read(WEB, name).replace("\r\n", "\n")
                native_source = read(NATIVE, name).replace("\r\n", "\n")
                if name == "MobAttackResultTracker.cs":
                    web_source = web_source.replace("\t\tpublic const byte Preparing = 4;\n", "")
                else:
                    web_source = web_source.replace('\t\t\t\tcase MobAttackResultTracker.Preparing: return "PREPARING";\n', "")
                self.assertEqual(
                    web_source,
                    native_source,
                    f"Web {name} must match the native shared combat helper",
                )

    def test_each_game_has_native_ack_retry_and_lane_isolation(self):
        for root_name, root in ROOTS:
            for game, lane in (("Game1", 1), ("Game2", 2)):
                with self.subTest(root=root_name, game=game):
                    service = read(root, f"{game}/Service.cs")
                    self.assertIn("Action<MobAttackResult> resultCallback", service, "Service callback overload missing")
                    self.assertIn(f"MobAttackResultTracker.BeginFor({lane},", service, "Service does not register the correct lane")
                    self.assertIn(f"MobAttackResultTracker.CancelFor({lane},", service, "Service does not cancel failed sends")

                    controller = read(root, f"{game}/Controller.cs")
                    self.assertIn(f"MobAttackResultTracker.ClearFor({lane})", controller, "disconnect does not clear the lane")
                    self.assertIn("case -48:", controller, "server result packet is not parsed")
                    self.assertIn(f"MobAttackResultTracker.ResolveFor({lane}, result", controller, "result is not resolved in the correct lane")

                    character = read(root, f"{game}/Char.cs")
                    self.assertIn(f"MobAttackResultTracker.CanAttemptFor({lane},", character, "manual attack ignores server retry window")
                    self.assertIn("scheduleManualMobRetry(attackSkill, attackTarget, retryDelay)", character, "manual attack does not schedule retry")
                    self.assertIn("private void cancelPendingManualMobRetry()", character, "manual retry cancellation helper missing")
                    self.assertIn("private void scheduleManualMobRetry(Skill skill, Mob target, int delayMs)", character, "manual retry scheduling helper missing")
                    self.assertIn("private void updatePendingManualMobRetry()", character, "manual retry update helper missing")
                    self.assertIn("updatePendingManualMobRetry();", character, "manual retry is not processed by the character update loop")

                    modfunc = read(root, f"{game}/ModFunc.cs")
                    self.assertIn("TrySendPlayerAttack(mobs, new MyVector(), 1, delegate(MobAttackResult result)", modfunc, "auto-attack ignores server result")
                    self.assertIn("Service.gI().TryCharMove()", modfunc, "auto-attack does not send pending position")
                    self.assertIn("public void SetUsedSkill(Skill skill, int retryAfterMs)", modfunc, "retry result cannot update skill cooldown")

                    massacre = read(root, f"{game}/Mod/XMAP/PickMobController.cs")
                    self.assertIn(f"MobAttackResultTracker.CanAttemptFor({lane},", massacre, "massacre ignores server retry window")
                    self.assertIn("TrySendPlayerAttack(targetMobs, targetPlayers, (targetChar == null) ? 1 : 2)", massacre, "massacre target attack call missing")
                    self.assertIn("delegate(MobAttackResult result)", massacre, "massacre does not process mob result")

    def test_web_only_diagnostics_are_wired_to_both_lanes(self):
        self.assertTrue((WEB / "Assets/Scripts/Assembly-CSharp/ClientWebCombatDiagnostics.cs").exists(), "web-only diagnostics class is missing")
        trace = read(WEB, "ClientWebCombatDiagnostics.cs")
        for method in ("RecordSend", "RecordResult", "RecordHit", "RecordTeleportStart", "RecordTeleportConfirm", "DrawPanel"):
            with self.subTest(method=method):
                self.assertIn(f"{method}(", trace)
        for game, lane in (("Game1", 1), ("Game2", 2)):
            with self.subTest(game=game):
                service = read(WEB, f"{game}/Service.cs")
                controller = read(WEB, f"{game}/Controller.cs")
                character = read(WEB, f"{game}/Char.cs")
                correction = read(WEB, f"{game}/Assets/src/f/Controller2.cs")
                self.assertIn(f"ClientWebCombatDiagnostics.RecordSend({lane},", service)
                self.assertIn(f"ClientWebCombatDiagnostics.RecordResult({lane}, result)", controller)
                self.assertIn(f"ClientWebCombatDiagnostics.RecordHit({lane},", controller)
                self.assertIn(f"ClientWebCombatDiagnostics.RecordTeleportStart({lane},", character)
                self.assertIn(f"ClientWebCombatDiagnostics.RecordTeleportConfirm({lane},", correction)

    def test_tracker_keeps_account_lanes_separate_and_applies_retry_window(self):
        tracker = read(WEB, "MobAttackResultTracker.cs")
        harness = r"""
public static class TrackerChecks {
    public static void Run() {
        int firstCallbacks = 0, secondCallbacks = 0;
        int first = NRO.Combat.MobAttackResultTracker.BeginFor(1, 3, 9, result => firstCallbacks++);
        int second = NRO.Combat.MobAttackResultTracker.BeginFor(2, 3, 9, result => secondCallbacks++);
        if (!NRO.Combat.MobAttackResultTracker.HasPendingFor(1)
            || !NRO.Combat.MobAttackResultTracker.HasPendingFor(2))
            throw new Exception("pending attacks were shared across account lanes");
        if (!NRO.Combat.MobAttackResultTracker.ResolveFor(1,
            new NRO.Combat.MobAttackResult { Sequence = first, Status = NRO.Combat.MobAttackResultTracker.Accepted }, 1000)
            || firstCallbacks != 1 || NRO.Combat.MobAttackResultTracker.HasPendingFor(1)
            || !NRO.Combat.MobAttackResultTracker.HasPendingFor(2))
            throw new Exception("first lane result affected the wrong pending attack");
        if (!NRO.Combat.MobAttackResultTracker.ResolveFor(2,
            new NRO.Combat.MobAttackResult { Sequence = second, Status = NRO.Combat.MobAttackResultTracker.Cooldown, RetryAfterMs = 500 }, 1000)
            || secondCallbacks != 1)
            throw new Exception("second lane result did not invoke its callback");
        if (NRO.Combat.MobAttackResultTracker.CanAttemptFor(2, 3, 1499)
            || !NRO.Combat.MobAttackResultTracker.CanAttemptFor(2, 3, 1500)
            || !NRO.Combat.MobAttackResultTracker.CanAttemptFor(1, 3, 1000))
            throw new Exception("retry gate did not respect lane and server cooldown");
    }
}
"""
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory) / "CombatTrackerChecks.cs"
            source.write_text(tracker + "\n" + harness, encoding="utf-8-sig")
            command = (
                "$ErrorActionPreference='Stop'; Add-Type -Path '"
                + str(source).replace("'", "''")
                + "'; [TrackerChecks]::Run()"
            )
            result = subprocess.run(
                ["powershell", "-NoProfile", "-NonInteractive", "-Command", command],
                capture_output=True,
                text=True,
            )
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main(verbosity=2)
