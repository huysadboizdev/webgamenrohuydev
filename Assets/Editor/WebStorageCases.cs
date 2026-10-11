using System;
using System.IO;
public static class WebStorageCases
{
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "nro-web-save-"+Guid.NewGuid().ToString("N"));
        try {
            var one = new NroWeb.FileSaveStore(Path.Combine(root,"Tab1"));
            var two = new NroWeb.FileSaveStore(Path.Combine(root,"Tab2"));
            var bytes = new byte[2*1024*1024]; bytes[0]=255; bytes[bytes.Length-1]=128;
            one.Write("resource/path",bytes); two.Write("resource/path",new byte[]{42});
            if(one.Read("resource/path").Length!=bytes.Length || two.Read("resource/path")[0]!=42)throw new Exception("Large cache or tab isolation failed");
            one.Delete("resource/path");if(one.Read("resource/path")!=null || two.Read("resource/path")==null)throw new Exception("Delete affected other tab");
            one.Write("other",new byte[]{1});one.Clear();if(one.Read("other")!=null || two.Read("resource/path")==null)throw new Exception("Clear affected other tab");
            UnityEngine.Debug.Log("WebStorageCases PASS (2MB cache, per-tab read/delete/clear)");
        } finally { if(Directory.Exists(root))Directory.Delete(root,true); }
    }
}
