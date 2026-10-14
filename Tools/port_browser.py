from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
base = root / 'Assets/Scripts/Assembly-CSharp'

def block(text, signature):
    start = text.index(signature)
    brace = text.index('{', start)
    depth = 0
    quote = None
    escape = False
    for i in range(brace, len(text)):
        c = text[i]
        if quote:
            if escape: escape = False
            elif c == '\\': escape = True
            elif c == quote: quote = None
            continue
        if c in '\"\'': quote = c; continue
        if c == '{': depth += 1
        if c == '}':
            depth -= 1
            if not depth: return start, i+1, text[brace+1:i]
    raise ValueError(signature)

def coroutine(text, signature, name, args='', body_override=None):
    start, end, body = block(text, signature)
    original = text[start:end]
    body = body if body_override is None else body_override
    body = re.sub(r'Thread.Sleep\((\d+)\);', lambda m: 'yield return new UnityEngine.WaitForSecondsRealtime('+str(int(m[1])/1000)+'f);', body)
    body = body.replace('return;', 'yield break;')
    call = f'NroWeb.BrowserHost.Run({name}Browser({args}));'
    browser = signature+' { '+call+' }\nprivate '+('static ' if 'static ' in signature else '')+'System.Collections.IEnumerator '+name+'Browser('+signature.split('(',1)[1].rsplit(')',1)[0]+') {\n'+body+'\n}'
    return text[:start]+'\n#if UNITY_WEBGL && !UNITY_EDITOR\n'+browser+'\n#else\n'+original+'\n#endif\n'+text[end:]

for tab in ('Game1', 'Game2'):
    d = base / tab
    p = d/'Main.cs'; s = p.read_text(encoding='utf-8-sig')
    old = s[s.index('if (Thread.CurrentThread.Name != "Main")'):s.index('started = true;')]
    new = '''#if UNITY_WEBGL && !UNITY_EDITOR
            mainThreadName = "Main";
            isIPhone = IphoneVersionApp = NroWeb.BrowserPlatform.IsMobile;
            isPC = !isIPhone;
            GameCanvas.isTouch = isIPhone;
#else
            '''+old+'''#endif
            '''
    s = s.replace(old,new,1)
    s = s.replace('Screen.SetResolution(720, 320, fullscreen: false);','if (!NroWeb.BrowserPlatform.IsBrowser) Screen.SetResolution(720, 320, fullscreen: false);')
    s = s.replace('Screen.SetResolution(1024, 600, fullscreen: false);','if (!NroWeb.BrowserPlatform.IsBrowser) Screen.SetResolution(1024, 600, fullscreen: false);')
    s = s.replace('Screen.fullScreen = true;', 'if (!NroWeb.BrowserPlatform.IsBrowser) Screen.fullScreen = true;').replace('Screen.fullScreen = false;', 'if (!NroWeb.BrowserPlatform.IsBrowser) Screen.fullScreen = false;')
    s = s.replace('IMEI = GetMacAddress();', 'IMEI = NroWeb.BrowserPlatform.IsBrowser ? "WEB-" + SystemInfo.deviceUniqueIdentifier : GetMacAddress();')
    # Never create native threads, network interfaces or Thread.Name dependencies on web.
    st,en,body = block(s,'public string GetMacAddress()')
    s = s[:st]+'public string GetMacAddress() {\n#if UNITY_WEBGL && !UNITY_EDITOR\nreturn "WEB-" + SystemInfo.deviceUniqueIdentifier;\n#else\n'+body+'\n#endif\n}'+s[en:]
    s = s.replace('Session_ME.update();\n\t\t\tSession_ME2.update();', '#if !UNITY_WEBGL || UNITY_EDITOR\nSession_ME.update();\nSession_ME2.update();\n#endif')
    s = s.replace('private void FixedUpdate()\n\t\t{','private void FixedUpdate()\n\t\t{\n#if UNITY_WEBGL && !UNITY_EDITOR\nSession_ME.update(); Session_ME2.update();\n#endif')
    p.write_text(s,encoding='utf-8')

    p=d/'GameScr.cs';s=p.read_text(encoding='utf-8-sig')
    s=coroutine(s,'public void RemoveAllItem()','RemoveAllItem')
    s=s.replace('(Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)','NroWeb.BrowserPlatform.IsMobile')
    p.write_text(s,encoding='utf-8')

    p=d/'ModFunc.cs';s=p.read_text(encoding='utf-8-sig')
    for signature,name in [('public void AutoPhaLe()','AutoPhaLe'),('private void BanVang()','BanVang'),('public void CollectAllThuongDe()','CollectAllThuongDe'),('private void DoAutoIncreasePoint()','DoAutoIncreasePoint'),('private void DoAutoIntrinsic()','DoAutoIntrinsic')]:
        s=coroutine(s,signature,name)
    # Await the nested sell loop instead of starting one per outer iteration.
    a,b,body=block(s,'private System.Collections.IEnumerator AutoPhaLeBrowser()')
    body=body.replace('BanVang();','yield return BanVangBrowser();')
    s=s[:a]+ 'private System.Collections.IEnumerator AutoPhaLeBrowser() {'+body+'}'+s[b:]
    s=coroutine(s,'public void AutoBuyItem(int num, Item itemBuy)','AutoBuyItem','num, itemBuy', '''
        for(int i=0;i<num;i++) { Service.gI().buyItem(3,itemBuy.template.id,0); yield return new UnityEngine.WaitForSecondsRealtime(0.2f); }
        GameScr.info1.addInfo("Đã mua xong.",0);
    ''')
    # DoFilter is called on the game update loop; it already handles one item per call.
    s=s.replace('Thread.Sleep(50);','#if !UNITY_WEBGL || UNITY_EDITOR\nThread.Sleep(50);\n#endif')
    for call in ['PickMob.GoBack','DoAutoIncreasePoint','DoAutoIntrinsic']:
        s=s.replace('new Thread('+call+').Start();','#if UNITY_WEBGL && !UNITY_EDITOR\n'+call+'();\n#else\nnew Thread('+call+').Start();\n#endif')
    p.write_text(s,encoding='utf-8')

    p=d/'Mod/XMAP/PickMob.cs';s=p.read_text(encoding='utf-8-sig')
    s=coroutine(s,'public static void GoBack()','GoBack')
    a,b,body=block(s,'private static System.Collections.IEnumerator GoBackBrowser()')
    # Exit auto travel after disconnect or user disables goback.
    body=body.replace('while (TileMap.mapID != mapGoback)','while (TileMap.mapID != mapGoback && isGoBack && Session_ME.gI().isConnected())')
    body=body.replace('while (TileMap.zoneID != zoneGoback)','while (TileMap.zoneID != zoneGoback && isGoBack && Session_ME.gI().isConnected())')
    s=s[:a]+'private static System.Collections.IEnumerator GoBackBrowser() {'+body+'}'+s[b:]
    p.write_text(s,encoding='utf-8')

    p=d/'CrackBallScr.cs';s=p.read_text(encoding='utf-8-sig')
    _,_,body=block(s,'public void AutoCrackBall()')
    # BrowserHost.Guard handles exceptions outside iterator MoveNext.
    locals_body = body[:body.index('try')].strip()
    body=locals_body + '\n' + body[body.index('while (isAutoCrackBall'):body.rindex('catch (Exception)')]
    body=body[:body.rfind('}')]
    body=body.replace('Thread.ResetAbort();','')
    body=body.replace('while (num < 7 && step != 5)','while (num < 7 && step != 5 && isAutoCrackBall)')
    body += '\n yield return null;\n' if False else ''
    # Empty while iterations must yield too (step transitions arrive asynchronously).
    last=body.rfind('}')
    body=body[:last]+'yield return null;\n'+body[last:]
    s=coroutine(s,'public void AutoCrackBall()','AutoCrackBall',body_override=body)
    s=s.replace('new Thread(AutoCrackBall).Start();','#if UNITY_WEBGL && !UNITY_EDITOR\nAutoCrackBall();\n#else\nnew Thread(AutoCrackBall).Start();\n#endif')
    p.write_text(s,encoding='utf-8')

    for file,call in [('InfoMe.cs','ModFunc.GI().CollectAllThuongDe'),('Panel.cs','ModFunc.GI().AutoPhaLe')]:
        p=d/file;s=p.read_text(encoding='utf-8-sig')
        s=s.replace('new Thread('+call+').Start();','#if UNITY_WEBGL && !UNITY_EDITOR\n'+call+'();\n#else\nnew Thread('+call+').Start();\n#endif')
        p.write_text(s,encoding='utf-8')

    for file in ('Rms.cs','Sound.cs','TField.cs'):
        p=d/file;s=p.read_text(encoding='utf-8-sig')
        s=s.replace('Thread.CurrentThread.Name == Main.mainThreadName','NroWeb.BrowserPlatform.IsBrowser || Thread.CurrentThread.Name == Main.mainThreadName')
        # Negated original TField expression is already parenthesized.
        p.write_text(s,encoding='utf-8')

    p=d/'TField.cs';s=p.read_text(encoding='utf-8-sig')
    s=s.replace('public void update()\n\t\t{','''public void update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (currentTField == this && isFocus && NroWeb.BrowserPlatform.InputState != 0) {
                setText(NroWeb.BrowserPlatform.InputValue);
                if (NroWeb.BrowserPlatform.InputState == 2) {
                    NroWeb.BrowserPlatform.CloseInput();
                    if (cmdDoneAction != null) cmdDoneAction.performAction();
                }
            }
#endif''')
    marker='''if (!Main.isPC || Main.isIPhone)
            {'''
    # Match source tabs flexibly.
    s=re.sub(r'if \(!Main.isPC \|\| Main.isIPhone\)\s*\{\s*kb = TouchScreenKeyboard.Open', '''if (!Main.isPC || Main.isIPhone)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                NroWeb.BrowserPlatform.OpenInput(currentTField.text, secure, inputType == INPUT_TYPE_NUMERIC);
                return;
#endif
                kb = TouchScreenKeyboard.Open''', s)
    # Closing focus must not leave the browser overlay attached to an old tab/field.
    s=s.replace('public void setFocus(bool isFocus)\n\t\t{','public void setFocus(bool isFocus)\n\t\t{\nif (!isFocus && currentTField == this) NroWeb.BrowserPlatform.CloseInput();')
    p.write_text(s,encoding='utf-8')

print('Browser platform and coroutine branches installed for both tabs')
