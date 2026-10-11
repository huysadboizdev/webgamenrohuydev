using System;
using System.Text;

/// <summary>Single-line insertion shared by both client tabs.</summary>
public static class ClipboardText
{
    public static string Insert(string text, int caret, string clipboard, int inputType,
        int maxLength, out int newCaret)
    {
        text = text ?? string.Empty;
        caret = Math.Max(0, Math.Min(caret, text.Length));
        newCaret = caret;
        int room = Math.Max(0, maxLength - text.Length);
        if (string.IsNullOrEmpty(clipboard) || room == 0) return text;
        var inserted = new StringBuilder();
        for (int i = 0; i < clipboard.Length && inserted.Length < room; i++)
        {
            char c = clipboard[i];
            if (c == '\r' || c == '\n' || c == '\t' || c == '\u2028' || c == '\u2029')
            {
                if (c == '\r' && i + 1 < clipboard.Length && clipboard[i + 1] == '\n') i++;
                c = ' ';
            }
            else if (char.IsControl(c)) continue;
            if (inputType == 1 && (c < '0' || c > '9')) continue;
            if (inputType == 3 && !((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')
                || (c >= 'a' && c <= 'z'))) continue;
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= clipboard.Length || !char.IsLowSurrogate(clipboard[i + 1])) continue;
                if (room - inserted.Length < 2) break;
                inserted.Append(c).Append(clipboard[++i]);
            }
            else if (!char.IsLowSurrogate(c)) inserted.Append(c);
        }
        newCaret = caret + inserted.Length;
        return text.Insert(caret, inserted.ToString());
    }
}
