using System;

public static class ClipboardTextTests
{
    private static int cases;
    private static void Check(string before, int caret, string paste, int type, int max,
        string expected, int expectedCaret)
    {
        int afterCaret;
        string actual = ClipboardText.Insert(before, caret, paste, type, max, out afterCaret);
        if (actual != expected || afterCaret != expectedCaret)
            throw new Exception("Paste case " + cases + " failed: " + actual + " / " + afterCaret);
        cases++;
    }
    public static void Run()
    {
        Check("", 0, "i 556 1 6:100000 35:1 103:7", 0, 80, "i 556 1 6:100000 35:1 103:7", 27);
        Check("ac", 1, "b", 0, 80, "abc", 2);
        Check("", 0, "Nguyệt ấn +7% KI", 0, 80, "Nguyệt ấn +7% KI", 16);
        Check("", 0, "a\r\nb\tc\nd\0\u0001", 0, 80, "a b c d", 7);
        Check("12", 1, "a3 -4.5", 1, 5, "13452", 4);
        Check("", 0, "aB09-đ_", 3, 80, "aB09", 4);
        Check("", 0, "Ab!@ 123", 2, 80, "Ab!@ 123", 8);
        Check("ab", 1, "XYZ", 0, 4, "aXYb", 3);
        Check("full", 2, "x", 0, 4, "full", 2);
        Check("abc", 1, "", 0, 80, "abc", 1);
        Check("abc", 1, null, 0, 80, "abc", 1);
        Check("a", 999, "b", 0, 80, "ab", 2);
        Check("a", -10, "b", 0, 80, "ba", 1);
        Check("", 0, "\ud83d\ude00", 0, 1, "", 0);
        Check("", 0, "\ud83d\ude00", 0, 2, "\ud83d\ude00", 2);
        Check("", 0, "\ud800x\udc00", 0, 80, "x", 1);
        Console.WriteLine("PASS clipboard insertion: " + cases + " cases");
    }
    public static void Main() { Run(); }
}
