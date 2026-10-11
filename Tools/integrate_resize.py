from pathlib import Path
root=Path(__file__).resolve().parents[1]
for tab in ['Game1','Game2']:
    p=root/'Assets/Scripts/Assembly-CSharp'/tab/'Main.cs';s=p.read_text(encoding='utf-8')
    s=s.replace('started = true;',f'''started = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("NRO_WEB_DEVICE {tab} pc=" + isPC + " mobile=" + isIPhone);
#endif''',1)
    # Recalculate bounds when browser resizes, preserving account/map state and resource zoom.
    s=s.replace('public void setsizeChange()\n\t\t{','''public void setsizeChange()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (isRun && (ScaleGUI.WIDTH != Screen.width || ScaleGUI.HEIGHT != Screen.height)) {
                ScaleGUI.initScaleGUI();
                GameCanvas.w = MotherCanvas.instance.getWidthz();
                GameCanvas.h = MotherCanvas.instance.getHeightz();
                GameCanvas.hw = GameCanvas.w / 2;
                GameCanvas.hh = GameCanvas.h / 2;
                if (GameCanvas.currentScreen is GameScr && TileMap.tmw > 0) GameScr.loadCamera(false, -1, -1);
                if (GameCanvas.currentScreen is LoginScr) ((LoginScr)GameCanvas.currentScreen).ResizeBrowser();
            }
#endif''')
    p.write_text(s,encoding='utf-8')
    p=root/'Assets/Scripts/Assembly-CSharp'/tab/'LoginScr.cs';s=p.read_text(encoding='utf-8')
    signature='public override void switchToMe()'
    code='''public void ResizeBrowser() {
            if (tfUser == null || tfPass == null) return;
            int shiftX = GameCanvas.w / 2 - (tfUser.x + tfUser.width / 2);
            int shiftY = GameCanvas.hh - 10 - tfUser.y;
            tfUser.x += shiftX; tfPass.x += shiftX;
            tfUser.y += shiftY; tfPass.y += shiftY;
            xLog += shiftX; yLog += shiftY;
            if (cmdLogin != null) { cmdLogin.x += shiftX; cmdLogin.y += shiftY; }
            if (cmdOK != null) { cmdOK.x += shiftX; cmdOK.y += shiftY; }
            if (ModFunc.cmdAccManager != null) { ModFunc.cmdAccManager.x += shiftX; ModFunc.cmdAccManager.y += shiftY; }
        }

        '''
    s=s.replace(signature,code+signature,1);p.write_text(s,encoding='utf-8')
    p=root/'Assets/Scripts/Assembly-CSharp'/tab/'TField.cs';s=p.read_text(encoding='utf-8')
    s=s.replace('public void setFocus(bool isFocus)\n\t\t{','public void setFocus(bool isFocus)\n\t\t{\nif (!isFocus && currentTField == this) NroWeb.BrowserPlatform.CloseInput();',1)
    p.write_text(s,encoding='utf-8')
print('Resize and platform startup diagnostics integrated')
