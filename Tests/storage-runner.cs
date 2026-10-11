using System;
using System.IO;
class StorageRunner
{
    static int Main() {
        var type=typeof(StorageRunner).Assembly.GetType("NroWeb.FileSaveStore");
        if(type==null){Console.WriteLine("FAIL browser save store missing");return 1;}
        string root=Path.Combine(Path.GetTempPath(),"nro-web-test-"+Guid.NewGuid().ToString("N"));
        try {
            dynamic one=Activator.CreateInstance(type,new object[]{Path.Combine(root,"Tab1")});
            dynamic two=Activator.CreateInstance(type,new object[]{Path.Combine(root,"Tab2")});
            var bytes=new byte[2*1024*1024];bytes[0]=255;bytes[bytes.Length-1]=128;
            one.Write("resource/path",bytes);two.Write("resource/path",new byte[]{42});
            byte[] data=one.Read("resource/path");byte[] other=two.Read("resource/path");
            if(data.Length!=bytes.Length || data[0]!=255 || data[data.Length-1]!=128 || other[0]!=42)throw new Exception("Large save or isolation");
            one.Delete("resource/path");if(one.Read("resource/path")!=null || two.Read("resource/path")==null)throw new Exception("Delete isolation");
            one.Write("another",new byte[]{1});one.Clear();if(one.Read("another")!=null || two.Read("resource/path")==null)throw new Exception("Clear isolation");
            Console.WriteLine("PASS storage 2MB resource cache and tab isolation");return 0;
        } finally {if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
