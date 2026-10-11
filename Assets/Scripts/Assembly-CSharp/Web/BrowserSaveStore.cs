using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
namespace NroWeb
{
    public static class BrowserSaveStore
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void NroSyncSaves();
#endif
        static FileSaveStore Store(string tab) { return new FileSaveStore(Path.Combine(Application.persistentDataPath,"nro-"+tab)); }
        static void Sync() {
#if UNITY_WEBGL && !UNITY_EDITOR
            NroSyncSaves();
#endif
        }
        public static void Write(string tab,string key,byte[] data) { Store(tab).Write(key,data);Sync(); }
        public static byte[] Read(string tab,string key) { return Store(tab).Read(key); }
        public static void Delete(string tab,string key) { Store(tab).Delete(key);Sync(); }
        public static void Clear(string tab) { Store(tab).Clear();Sync(); }
    }
}
