using System.Collections;
using UnityEngine;

namespace NroWeb
{
    public sealed class BrowserHost : MonoBehaviour
    {
        private static BrowserHost instance;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (instance != null) return;
            var host = new GameObject("BrowserHost");
            instance = host.AddComponent<BrowserHost>();
            DontDestroyOnLoad(host);
            Management.IpServer = BrowserPlatform.ServerList;
            // Let the focused HTML field receive typing; canvas focus still receives game keys.
            WebGLInput.captureAllKeyboardInput = false;
            Application.runInBackground = true;
            Time.fixedDeltaTime = 1f / 30f;
#endif
        }
        public static void Run(IEnumerator routine)
        {
            if (instance == null) { var host = new GameObject("BrowserHost"); instance = host.AddComponent<BrowserHost>(); DontDestroyOnLoad(host); }
            instance.StartCoroutine(Guard(routine));
        }
        private static IEnumerator Guard(IEnumerator routine)
        {
            while (true) {
                bool next;
                try { next = routine.MoveNext(); }
                catch (System.Exception error) { Debug.LogException(error); next = false; }
                if (!next) yield break;
                yield return routine.Current;
            }
        }
        public void SelectTab(string value)
        {
            int tab;
            if (!int.TryParse(value, out tab) || tab < 0 || tab > 1) return;
            Management.ChangeTab(tab);
        }
        public static void TabChanged(int tab)
        {
            BrowserPlatform.CloseInput();
            Game1.ipKeyboard.CancelBrowser(); Game2.ipKeyboard.CancelBrowser();
            if (Game1.TField.currentTField != null) Game1.TField.currentTField.setFocus(false);
            if (Game2.TField.currentTField != null) Game2.TField.currentTField.setFocus(false);
            Game1.GameCanvas.clearKeyHold(); Game1.GameCanvas.clearKeyPressed();
            Game2.GameCanvas.clearKeyHold(); Game2.GameCanvas.clearKeyPressed();
            Game1.GameCanvas.isPointerDown = Game1.GameCanvas.isPointerClick = Game1.GameCanvas.isPointerJustDown = Game1.GameCanvas.isPointerMove = false;
            Game2.GameCanvas.isPointerDown = Game2.GameCanvas.isPointerClick = Game2.GameCanvas.isPointerJustDown = Game2.GameCanvas.isPointerMove = false;
            Game1.mScreen.keyTouch = Game2.mScreen.keyTouch = -1;
            BrowserPlatform.NotifyTabChanged(tab);
        }
        public void Paste(string text)
        {
            if (Management.tab == TabType.Tab1 && Game1.TField.currentTField != null) Game1.TField.currentTField.pasteText(text);
            if (Management.tab == TabType.Tab2 && Game2.TField.currentTField != null) Game2.TField.currentTField.pasteText(text);
        }
        private void OnApplicationFocus(bool focused) { if (!focused) PlayerPrefs.Save(); }
        private void OnApplicationQuit() { PlayerPrefs.Save(); }
    }
}
