from pathlib import Path
import importlib.util

root = Path(__file__).resolve().parents[1]
base = root / 'Assets/Scripts/Assembly-CSharp'
reference = root.parent / 'scriptsweb-replacement-backup-20261014-081552/Assets/Scripts/Assembly-CSharp'
# Read only the method parser; port_browser has top-level mutations.
parser = (root / 'Tools/port_browser.py').read_text(encoding='utf-8').split("for tab in ('Game1', 'Game2'):")[0]
scope = {'__file__': str(root / 'Tools/port_browser.py')}
exec(parser, scope)
block = scope['block']

def method(source, signature):
    start, end, _ = block(source, signature)
    return source[start:end]

p = base / 'Management.cs'
s = p.read_text(encoding='utf-8-sig')
old = (reference / 'Management.cs').read_text(encoding='utf-8-sig')
s = s.replace('public static TabType tab;', 'public static TabType tab;\n    private static int lastTabChangeFrame = -1;\n\n    ' + method(old, 'public static bool CanProcessInput(TabType inputTab)'))
start,end,_ = block(s, 'public static void ChangeTab(int index)')
s = s[:start] + method(old, 'public static void ChangeTab(int index)') + s[end:]
p.write_text(s, encoding='utf-8')

for tab in ('Game1', 'Game2'):
    d = base / tab
    ref = reference / tab
    p = d / 'Main.cs'; s = p.read_text(encoding='utf-8-sig')
    s = s.replace('&& paintCount <= updateCount)', '&& (NroWeb.BrowserPlatform.IsBrowser || paintCount <= updateCount))')
    tabtype = 'Tab1' if tab == 'Game1' else 'Tab2'
    s = s.replace('if (Management.tab != TabType.' + tabtype + ') return;', 'if (!Management.CanProcessInput(TabType.' + tabtype + ')) return;')
    s = s.replace('GameCanvas.hh = GameCanvas.h / 2;', 'GameCanvas.hh = GameCanvas.h / 2;\n                GameScr.RefreshBrowserControls();', 1)
    s = s.replace('((LoginScr)GameCanvas.currentScreen).ResizeBrowser();', '((LoginScr)GameCanvas.currentScreen).ResizeBrowser();\n                if (GameCanvas.currentScreen is ServerScr) ((ServerScr)GameCanvas.currentScreen).ResizeBrowser();\n                if (GameCanvas.currentScreen is ServerListScreen) ((ServerListScreen)GameCanvas.currentScreen).ResizeBrowser();', 1)
    p.write_text(s, encoding='utf-8')
    for file, signature, anchor in [('GameScr.cs','public static void RefreshBrowserControls()','public static void setSkillBarPosition()'), ('ServerScr.cs','public void ResizeBrowser()','private void sort()'), ('ServerListScreen.cs','public void ResizeBrowser()','public static void doUpdateServer()')]:
        p=d/file; s=p.read_text(encoding='utf-8-sig')
        s=s.replace(anchor, method((ref/file).read_text(encoding='utf-8-sig'),signature)+'\n\n        '+anchor, 1)
        if file == 'GameScr.cs':
            s=s.replace('if (gamePad != null) gamePad.release();', 'GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();\n            GameCanvas.isPointerDown = GameCanvas.isPointerClick = GameCanvas.isPointerJustDown = GameCanvas.isPointerMove = false;')
        if file == 'ServerScr.cs':
            s=s.replace('numh = vecServer.size() / numw', 'if (NroWeb.BrowserPlatform.IsBrowser) numw = System.Math.Max(1, System.Math.Min(numw, System.Math.Min(vecServer.size(), (GameCanvas.w + w2c) / (wc + w2c))));\n            numh = vecServer.size() / numw',1)
        p.write_text(s,encoding='utf-8')
    p=d/'TField.cs'; s=p.read_text(encoding='utf-8-sig')
    line='if (!isFocus && currentTField == this) NroWeb.BrowserPlatform.CloseInput();'
    s=s.replace(line+'\n'+line,line)
    # Enter on a browser field follows the same command as mobile Done.
    # Native keyboard and clipboard editing remain intact.
    s=s.replace('public void setFocusWithKb(bool isFocus)\n\t\t{','public void setFocusWithKb(bool isFocus)\n\t\t{\n            if (!isFocus && currentTField == this) NroWeb.BrowserPlatform.CloseInput();')
    p.write_text(s,encoding='utf-8')
    p=d/'ipKeyboard.cs';s=p.read_text(encoding='utf-8-sig')
    s=s.replace('act = action;', '''act = action;
#if UNITY_WEBGL && !UNITY_EDITOR
            NroWeb.BrowserPlatform.OpenInput(text, type == PASS, type == NUMBERIC);
            return;
#endif''',1)
    s=s.replace('public static void update()\n\t\t{', '''public static void update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (act != null && NroWeb.BrowserPlatform.InputState == 2) {
                string value = NroWeb.BrowserPlatform.InputValue;
                Command pending = act;
                act = null;
                NroWeb.BrowserPlatform.CloseInput();
                pending.perform(value);
            }
            return;
#endif''',1)
    p.write_text(s,encoding='utf-8')
    p=d/'TField.cs';s=p.read_text(encoding='utf-8')
    s=s.replace('if (currentTField == this && isFocus && NroWeb.BrowserPlatform.InputState != 0)', f'if (Management.tab == TabType.{tabtype} && currentTField == this && isFocus && NroWeb.BrowserPlatform.HasFieldInput && NroWeb.BrowserPlatform.InputState != 0)')
    p.write_text(s,encoding='utf-8')
    p=d/'ipKeyboard.cs';s=p.read_text(encoding='utf-8')
    s=s.replace('NroWeb.BrowserPlatform.OpenInput(text, type == PASS, type == NUMBERIC);','NroWeb.BrowserPlatform.OpenInput(text, type == PASS, type == NUMBERIC, false);')
    s=s.replace('if (act != null && NroWeb.BrowserPlatform.InputState == 2)',f'if (Management.tab == TabType.{tabtype} && act != null && NroWeb.BrowserPlatform.HasCommandInput && NroWeb.BrowserPlatform.InputState == 2)')
    s=s.replace('public static void update()', 'public static void CancelBrowser() { if (NroWeb.BrowserPlatform.IsBrowser) act = null; }\n\n        public static void update()',1)
    p.write_text(s,encoding='utf-8')

p=base/'Web/BrowserPlatform.cs';s=p.read_text(encoding='utf-8')
s=s.replace('public static void OpenInput(string text, bool password, bool numeric) {','private static int inputOwner;\n        public static bool HasFieldInput { get { return inputOwner == 1; } }\n        public static bool HasCommandInput { get { return inputOwner == 2; } }\n        public static void OpenInput(string text, bool password, bool numeric, bool field = true) {\n            inputOwner = field ? 1 : 2;')
s=s.replace('public static void CloseInput() {','public static void CloseInput() {\n            inputOwner = 0;')
p.write_text(s,encoding='utf-8')
p=base/'Web/BrowserHost.cs';s=p.read_text(encoding='utf-8').replace('BrowserPlatform.CloseInput();\n            if (Game1.TField','BrowserPlatform.CloseInput();\n            Game1.ipKeyboard.CancelBrowser(); Game2.ipKeyboard.CancelBrowser();\n            if (Game1.TField',1)
p.write_text(s,encoding='utf-8')
print('Native browser shell integration installed')
