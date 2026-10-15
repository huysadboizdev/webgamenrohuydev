"""Exercise the real Mob.GetFrame method without building the Unity application."""
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


def method(source, signature):
    start = source.index(signature)
    end = source.index("{", start) + 1
    depth = 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


class MobFrameRecoveryChecks(unittest.TestCase):
    def test_both_tabs_recover_from_missing_and_invalid_frames(self):
        for game in ("Game1", "Game2"):
            with self.subTest(game=game):
                source = (ROOT / f"Assets/Scripts/Assembly-CSharp/{game}/Mob.cs").read_text(encoding="utf-8-sig")
                methods = method(source, "public void GetFrame()") + method(source, "private bool isTypeNewMod()")
                harness = r'''
using System;
using System.Collections;
using System.Collections.Generic;
public class Frame { }
public class EffectData { public int typeData = 2; public Frame[] frame = new Frame[15]; }
public class Template { public EffectData data = new EffectData(); }
public class Table { public Hashtable h = new Hashtable(); public object get(object k) { return h[k]; } }
public class Controller { public static Table frameHT_NEWBOSS = new Table(); }
public class Mob {
    public static Template[] arrMobTemplate = new Template[96];
    private bool isGetFr = true;
    public int templateId = 95;
    private int[][] frameArr;
    private int[] stand = new int[] { 0 }, move = new int[] { 1 }, moveFast = new int[] { 2 },
        attack1 = new int[] { 4 }, attack2 = new int[] { 7 }, hurt = new int[] { 11 };
    METHODS
    public static void Run() {
        arrMobTemplate[95] = new Template();
        for (int i = 0; i < 15; i++) arrMobTemplate[95].data.frame[i] = new Frame();
        int[][] good = new int[][] { new int[] { 1, 0 }, new int[] { 2 }, new int[] { 3 },
            new int[] { 5 }, new int[] { 8 }, new int[] { 11 } };
        int[][][] bad = new int[][][] { null, new int[0][], new int[5][],
            new int[][] { null, new int[] { 1 }, new int[] { 2 }, new int[] { 4 }, new int[] { 7 }, new int[] { 11 } },
            new int[][] { new int[0], new int[] { 1 }, new int[] { 2 }, new int[] { 4 }, new int[] { 7 }, new int[] { 11 } },
            new int[][] { new int[] { -1 }, new int[] { 1 }, new int[] { 2 }, new int[] { 4 }, new int[] { 7 }, new int[] { 11 } },
            new int[][] { new int[] { 15 }, new int[] { 1 }, new int[] { 2 }, new int[] { 4 }, new int[] { 7 }, new int[] { 11 } } };
        var invalidTables = new List<int[][]>(bad);
        for (int row = 0; row < 6; row++) {
            foreach (int[] invalid in new int[][] { null, new int[0], new int[] { -1 }, new int[] { 15 } }) {
                var table = (int[][])good.Clone(); table[row] = invalid; invalidTables.Add(table);
            }
        }
        foreach (int[][] table in invalidTables) {
            Controller.frameHT_NEWBOSS.h.Clear();
            if (table != null) Controller.frameHT_NEWBOSS.h["95"] = table;
            var mob = new Mob();
            int characterTicks = 0, effectTicks = 0;
            for (int tick = 0; tick < 3; tick++) { characterTicks++; mob.GetFrame(); effectTicks++; }
            if (characterTicks != 3 || effectTicks != 3 || mob.stand[0] != 0 || mob.hurt[0] != 11)
                throw new Exception("Missing/invalid custom data broke downstream updates or default sequences");
            Controller.frameHT_NEWBOSS.h["95"] = good;
            mob.GetFrame();
            if (!Object.ReferenceEquals(mob.stand, good[0]) || !Object.ReferenceEquals(mob.move, good[1])
                || !Object.ReferenceEquals(mob.moveFast, good[2]) || !Object.ReferenceEquals(mob.attack1, good[3])
                || !Object.ReferenceEquals(mob.attack2, good[4]) || !Object.ReferenceEquals(mob.hurt, good[5]))
                throw new Exception("Valid frame table did not recover after missing/invalid data");
        }
        Controller.frameHT_NEWBOSS.h["95"] = good;
        arrMobTemplate[95].data.frame = null;
        var waiting = new Mob(); waiting.GetFrame();
        arrMobTemplate[95].data.frame = new Frame[15];
        for (int i = 0; i < 15; i++) arrMobTemplate[95].data.frame[i] = new Frame();
        waiting.GetFrame();
        if (!Object.ReferenceEquals(waiting.stand, good[0])) throw new Exception("Sprite data did not recover");
        arrMobTemplate[95].data.typeData = 0;
        var ordinary = new Mob(); ordinary.GetFrame();
        if (ordinary.stand[0] != 0) throw new Exception("Ordinary animations changed");
    }
}
'''.replace("METHODS", methods)
                with tempfile.TemporaryDirectory() as directory:
                    cs = Path(directory) / "MobFrameRecovery.cs"
                    cs.write_text(harness, encoding="utf-8-sig")
                    command = "Add-Type -Path '" + str(cs).replace("'", "''") + "'; [Mob]::Run()"
                    result = subprocess.run(["powershell", "-NoProfile", "-NonInteractive", "-Command", command], capture_output=True, text=True)
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main(verbosity=2)
