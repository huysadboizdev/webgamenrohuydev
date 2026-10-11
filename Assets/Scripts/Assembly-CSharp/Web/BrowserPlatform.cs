using System.Runtime.InteropServices;
using UnityEngine;

namespace NroWeb
{
    public static class BrowserPlatform
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int NroDeviceKind();
        [DllImport("__Internal")] private static extern void NroTabChanged(int tab);
        [DllImport("__Internal")] private static extern string NroServerList();
        [DllImport("__Internal")] private static extern void NroInputOpen(string text, int password, int numeric);
        [DllImport("__Internal")] private static extern string NroInputValue();
        [DllImport("__Internal")] private static extern int NroInputState();
        [DllImport("__Internal")] private static extern void NroInputClose();
#endif
        public static bool IsBrowser {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        public static bool IsMobile {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return NroDeviceKind() != 0;
#else
                return Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
#endif
            }
        }
        public static string ServerList {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return NroServerList();
#else
                return "NRO NGU:127.0.0.1:14445:0,0,0";
#endif
            }
        }
        public static void OpenInput(string text, bool password, bool numeric) {
#if UNITY_WEBGL && !UNITY_EDITOR
            NroInputOpen(text, password ? 1 : 0, numeric ? 1 : 0);
#endif
        }
        public static string InputValue {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return NroInputValue();
#else
                return "";
#endif
            }
        }
        public static int InputState {
            get {
#if UNITY_WEBGL && !UNITY_EDITOR
                return NroInputState();
#else
                return 0;
#endif
            }
        }
        public static void NotifyTabChanged(int tab) {
#if UNITY_WEBGL && !UNITY_EDITOR
            NroTabChanged(tab);
#endif
        }
        public static void CloseInput() {
#if UNITY_WEBGL && !UNITY_EDITOR
            NroInputClose();
#endif
        }
    }
}
