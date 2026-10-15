"""Web/PC checks for the additive boss-screen event (command 115).

Ported from the Android/APK notification checks, adapting only the repo path.
Command 93 keeps feeding the VIP ticker byte-for-byte; the structured screen
event (schema, version, eventKey, semanticBossId, form, mapId, mapType, ticker)
drives only the upper-right overlay. Drawing and hit testing share one filter;
unknown identities stay visible (fail-open). Runs without Unity.
"""
from pathlib import Path
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[1]
CSC = Path("C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe")
NOTICE_DATA = ROOT / "Assets/Scripts/Assembly-CSharp/BossScreenNoticeData.cs"


def block(source, marker):
    start = source.index(marker)
    begin = source.index("{", start)
    depth = 1
    end = begin + 1
    while depth:
        depth += (source[end] == "{") - (source[end] == "}")
        end += 1
    return source[start:end]


class BossScreenNotificationChecks(unittest.TestCase):
    def test_both_tabs_route_ticker_and_screen_event(self):
        for game in ("Game1", "Game2"):
            with self.subTest(game=game):
                folder = ROOT / "Assets/Scripts/Assembly-CSharp" / game
                controller = (folder / "Assets/src/f/Controller2.cs").read_text(encoding="utf-8-sig")

                command_93 = block(controller, "case 93:")
                self.assertIn("string str = msg.reader().readUTF();", command_93)
                self.assertLess(command_93.index("readUTF()"), command_93.index("Res.changeString(str)"))
                self.assertIn("GameScr.gI().chatVip(str);", command_93)
                self.assertNotIn("chatVip(str,", command_93)
                self.assertNotIn("readInt()", command_93)
                self.assertNotIn("readBoolean()", command_93)
                self.assertNotIn("available()", command_93)
                self.assertNotIn("0x424E4631", command_93)
                self.assertNotIn("hiddenFromScreenPanel", command_93)

                self.assertIn("case 115:", controller)
                command_115 = block(controller, "case 115:")
                self.assertIn("ReadBossScreenEvent(msg.reader())", command_115)
                self.assertIn("showBossScreenNotice(", command_115)
                self.assertNotIn("chatVip(", command_115)

                decoder = block(controller, "ReadBossScreenEvent(myReader reader)")
                self.assertIn("reader.mark(", decoder)
                self.assertIn("reader.reset();", decoder)
                self.assertIn("readByte()", decoder)
                self.assertIn("readUTF()", decoder)
                self.assertIn("readInt()", decoder)
                self.assertLess(decoder.index("readUTF()"), decoder.index("readInt()"))
                self.assertIn("ScreenSchema", decoder)
                self.assertIn("ScreenVersion", decoder)
                self.assertIn("return null;", decoder)

                game_scr = (folder / "GameScr.cs").read_text(encoding="utf-8-sig")
                self.assertIn("public void showBossScreenNotice(", game_scr)
                overlay = block(game_scr, "public void showBossScreenNotice(")
                self.assertIn("ModFunc.bossNotif.addElement(", overlay)
                self.assertNotIn("vChatVip.addElement", overlay)
                chat_vip = block(game_scr, "public void chatVip(string chatVip)")
                self.assertIn("vChatVip.addElement(chatVip);", chat_vip)
                # Legacy fallback: old server (no 115) must still feed the panel from 93.
                self.assertIn('chatVip.StartsWith("BOSS ")', chat_vip)
                self.assertIn("new ShowBoss(chatVip)", chat_vip)
                self.assertIn("hasBossNotifTicker", chat_vip)
                self.assertIn("removeLegacyBossNotifTicker", overlay)

                show_boss = (folder / "Mod/ShowBoss.cs").read_text(encoding="utf-8-sig")
                self.assertIn("public ShowBoss(string tickerText, string eventKey, int semanticBossId, int form, int mapId, int mapType)", show_boss)
                self.assertIn("BossScreenNoticeData.ShouldShow(", show_boss)
                self.assertIn("rawTicker", show_boss)
                self.assertIn("HasScreenIdentity", show_boss)

                mod_func = (folder / "ModFunc.cs").read_text(encoding="utf-8-sig")
                paint = block(mod_func, "public void Paint(mGraphics g)")
                self.assertIn("showBoss.ShouldShowOnScreen()", paint)
                touch = block(game_scr, "private void UpdateKeyTouchControl()")
                self.assertIn("showBoss.ShouldShowOnScreen()", touch)

    def test_notice_data_contract_matches_server(self):
        source = NOTICE_DATA.read_text(encoding="utf-8-sig")
        self.assertIn("ScreenCommand = 115", source)
        self.assertIn("ScreenSchema = 1", source)
        self.assertIn("ScreenVersion = 1", source)
        for boss_id in ("-233", "-234", "-235", "-236", "-237", "-238", "-343", "-341", "-342"):
            self.assertIn(boss_id, source)
        for dungeon_type in ("MapTypeDoanhTrai = 2", "MapTypeBanDoKhoBau = 4",
                             "MapTypeConDuongRanDoc = 6", "MapTypeKhiGasHuyDiet = 7"):
            self.assertIn(dungeon_type, source)
        for special_id in ("-365", "-78", "-77", "-799"):
            self.assertIn(special_id, source)
        self.assertIn("MapTypeMabu14H = 9", source)
        self.assertIn("OshinMapIdMin = 114", source)
        self.assertIn("OshinMapIdMax = 120", source)
        self.assertIn("public static bool ShouldShow(int semanticBossId, int mapId, int mapType)", source)

    def test_filter_matrix_and_decoder_fail_open(self):
        controller = (ROOT / "Assets/Scripts/Assembly-CSharp/Game1/Assets/src/f/Controller2.cs").read_text(encoding="utf-8-sig")
        decoder = block(controller, "private static BossScreenNoticeData.BossScreenEvent ReadBossScreenEvent(")
        shell = r'''
using System;
using System.Collections.Generic;
using System.Text;
class myReader {
    private readonly byte[] data; private int position; private int markedPosition;
    public myReader(byte[] value) { data = value; }
    public int available() { return data.Length - position; }
    public void mark(int readlimit) { markedPosition = position; }
    public void reset() { position = markedPosition; }
    public sbyte readByte() { if (position >= data.Length) throw new Exception("EOF"); return unchecked((sbyte)data[position++]); }
    public int readInt() { return (readByte() << 24) | (readByte() << 16) | (readByte() << 8) | readByte(); }
    public string readUTF() {
        int length = (readByte() << 8) | (((int)readByte()) & 255);
        byte[] bytes = new byte[length];
        for (int i = 0; i < length; i++) bytes[i] = (byte)readByte();
        return Encoding.UTF8.GetString(bytes);
    }
}
class ScreenReader {
    DECODER
    public static BossScreenNoticeData.BossScreenEvent Read(myReader reader) { return ReadBossScreenEvent(reader); }
}
class Test {
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static byte[] Packet(sbyte schema, sbyte version, string eventKey, int semanticId, int form, int mapId, int mapType, string ticker) {
        List<byte> packet = new List<byte>();
        packet.Add((byte)schema); packet.Add((byte)version);
        AddUtf(packet, eventKey);
        AddInt(packet, semanticId); AddInt(packet, form); AddInt(packet, mapId); AddInt(packet, mapType);
        AddUtf(packet, ticker);
        return packet.ToArray();
    }
    static void AddInt(List<byte> packet, int value) {
        packet.Add((byte)(value >> 24)); packet.Add((byte)(value >> 16)); packet.Add((byte)(value >> 8)); packet.Add((byte)value);
    }
    static void AddUtf(List<byte> packet, string value) {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        packet.Add((byte)(bytes.Length >> 8)); packet.Add((byte)bytes.Length);
        packet.AddRange(bytes);
    }
    static void Main() {
        Check(BossScreenNoticeData.ScreenCommand == 115, "screen command");
        Check(BossScreenNoticeData.ShouldShow(1000, 5, 0), "ordinary boss remains visible");
        Check(BossScreenNoticeData.ShouldShow(-233, 113, 0), "same 12h identity outside Oshin stays visible");
        Check(BossScreenNoticeData.ShouldShow(-233, 121, 0), "same 12h identity outside Oshin stays visible");
        int[] twelveHour = new int[] { -233, -234, -235, -236, -237, -238, -343, -341, -342 };
        foreach (int id in twelveHour) Check(!BossScreenNoticeData.ShouldShow(id, 114, 0), "Oshin 12h hidden " + id);
        foreach (int id in twelveHour) Check(!BossScreenNoticeData.ShouldShow(id, 120, 0), "Oshin 12h hidden " + id);
        Check(!BossScreenNoticeData.ShouldShow(-214, 200, 9), "Mabu 14h hidden");
        Check(!BossScreenNoticeData.ShouldShow(-348, 200, 9), "SuperBu 14h hidden");
        Check(BossScreenNoticeData.ShouldShow(-214, 200, 0), "Mabu outside event map stays visible");
        Check(BossScreenNoticeData.ShouldShow(-348, 200, 0), "SuperBu outside event map stays visible");
        foreach (int mapType in new int[] { 2, 4, 6, 7 }) Check(!BossScreenNoticeData.ShouldShow(1000, 50, mapType), "dungeon hidden " + mapType);
        Check(BossScreenNoticeData.ShouldShow(1000, 50, 1), "non-dungeon map type stays visible");
        foreach (int id in new int[] { -365, -78, -77, -799 }) Check(!BossScreenNoticeData.ShouldShow(id, 5, 0), "special hidden " + id);
        Check(BossScreenNoticeData.ShouldShow(9999, 5, 0), "unknown identity fails open");
        Check(BossScreenNoticeData.ShouldShow(1000, 9999, 99), "unknown location fails open");

        var legacy = new myReader(new byte[0]);
        Check(ScreenReader.Read(legacy) == null && legacy.available() == 0, "legacy empty packet");
        var truncated = new myReader(new byte[] { 1 });
        Check(ScreenReader.Read(truncated) == null && truncated.available() == 1, "truncated packet peeks safely");
        var unknownSchema = new myReader(Packet(99, 1, "k", 1000, 0, 5, 0, "t"));
        Check(ScreenReader.Read(unknownSchema) == null && unknownSchema.available() == unknownSchema.available(), "unknown schema ignored");
        var unknownVersion = new myReader(Packet(1, 99, "k", 1000, 0, 5, 0, "t"));
        int before = unknownVersion.available();
        Check(ScreenReader.Read(unknownVersion) == null && unknownVersion.available() == before, "unknown version ignored");
        var hidden = new myReader(Packet(1, 1, "-233:0:114:0", -233, 0, 114, 0, "BOSS X"));
        BossScreenNoticeData.BossScreenEvent hiddenEvent = ScreenReader.Read(hidden);
        Check(hiddenEvent != null, "hidden event decodes");
        Check(hiddenEvent.eventKey == "-233:0:114:0" && hiddenEvent.semanticBossId == -233 && hiddenEvent.form == 0 && hiddenEvent.mapId == 114 && hiddenEvent.mapType == 0 && hiddenEvent.ticker == "BOSS X", "field order round-trip");
        Check(!BossScreenNoticeData.ShouldShow(hiddenEvent.semanticBossId, hiddenEvent.mapId, hiddenEvent.mapType), "decoded hidden event filtered");
        Check(hidden.available() == 0, "valid packet consumed");
        var shown = new myReader(Packet(1, 1, "1000:0:5:0", 1000, 0, 5, 0, "BOSS Y"));
        BossScreenNoticeData.BossScreenEvent shownEvent = ScreenReader.Read(shown);
        Check(shownEvent != null && BossScreenNoticeData.ShouldShow(shownEvent.semanticBossId, shownEvent.mapId, shownEvent.mapType), "decoded ordinary event visible");
        Console.WriteLine("PASS filter matrix, wire order, fail-open decoder");
    }
}
'''.replace("DECODER", decoder)
        with tempfile.TemporaryDirectory(prefix="boss-screen-notification-") as directory:
            directory = Path(directory)
            source = directory / "Checks.cs"
            executable = directory / "Checks.exe"
            source.write_text(shell, encoding="utf-8-sig")
            subprocess.run([str(CSC), "/nologo", "/out:" + str(executable), str(source), str(NOTICE_DATA)], check=True, capture_output=True, text=True)
            result = subprocess.run([str(executable)], check=True, capture_output=True, text=True)
        self.assertIn("PASS filter matrix, wire order, fail-open decoder", result.stdout)


if __name__ == "__main__":
    unittest.main(verbosity=2)
