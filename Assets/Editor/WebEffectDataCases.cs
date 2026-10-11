using System;
using System.IO;
using UnityEngine;

public static class WebEffectDataCases
{
    static sbyte[] Bytes(string path) { byte[] b = File.ReadAllBytes(path); var s = new sbyte[b.Length]; Buffer.BlockCopy(b, 0, s, 0, b.Length); return s; }
    public static void Run()
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Tests/Fixtures/Effects"));
        if (!Directory.Exists(directory)) throw new Exception("Missing real effect fixtures: " + directory);
        int count = 0;
        foreach (string path in Directory.GetFiles(directory, "DataEffect_*")) {
            int id = int.Parse(Path.GetFileName(path).Substring(11));
            var data = Bytes(path);
            var first = new Game1.EffectData { ID = id };
            var second = new Game2.EffectData { ID = id };
            if (id == 60) { first.readDataNewBoss(data, 2); second.readDataNewBoss(data, 2); }
            else { first.readData(data); second.readData(data); }
            if (id >= 281 && id <= 284) {
                if (first.frame != null || second.frame != null) throw new Exception("Unsupported effect accepted: " + id);
                continue;
            }
            if (first.arrFrame == null || second.arrFrame == null) throw new Exception("Real effect parse failed: " + id);
            var firstFrames = first.frame; var secondFrames = second.frame;
            for (int n = 0; n < Math.Min(data.Length - 2, 64); n++) {
                var broken = new sbyte[n]; Array.Copy(data, broken, n);
                first.readData(broken); second.readData(broken);
                if (!ReferenceEquals(firstFrames, first.frame) || !ReferenceEquals(secondFrames, second.frame)) throw new Exception("Partial data replaced effect: " + id);
            }
            count++;
        }
        Debug.Log("WEB_EFFECT_REAL_DATA_PASS both tabs count=" + count + " invalid/truncated data rejected");
    }
}
