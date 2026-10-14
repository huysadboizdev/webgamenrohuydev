using System;
using UnityEngine;

namespace Game2
{
	public class ipKeyboard
	{
		private static TouchScreenKeyboard tk;

		public static int TEXT;

		public static int NUMBERIC = 1;

		public static int PASS = 2;

		private static Command act;

		public static void openKeyBoard(string caption, int type, string text, Command action)
		{
			act = action;
#if UNITY_WEBGL && !UNITY_EDITOR
            NroWeb.BrowserPlatform.OpenInput(text, type == PASS, type == NUMBERIC, false);
            return;
#endif
			TouchScreenKeyboardType keyboardType = ((type == 0 || type == 2) ? TouchScreenKeyboardType.ASCIICapable : TouchScreenKeyboardType.NumberPad);
			TouchScreenKeyboard.hideInput = false;
			tk = TouchScreenKeyboard.Open(text, keyboardType, autocorrection: false, multiline: false, type == 2, alert: false, caption);
		}

		public static void CancelBrowser() { if (NroWeb.BrowserPlatform.IsBrowser) act = null; }

        public static void update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Management.tab == TabType.Tab2 && act != null && NroWeb.BrowserPlatform.HasCommandInput && NroWeb.BrowserPlatform.InputState == 2) {
                string value = NroWeb.BrowserPlatform.InputValue;
                Command pending = act;
                act = null;
                NroWeb.BrowserPlatform.CloseInput();
                pending.perform(value);
            }
            return;
#endif
			try
			{
				if (tk != null && tk.done)
				{
					if (act != null)
					{
						act.perform(tk.text);
					}
					tk.text = string.Empty;
					tk = null;
				}
			}
			catch (Exception)
			{
			}
		}
	}
}
