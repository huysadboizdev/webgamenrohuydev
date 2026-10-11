using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NroWeb
{
    // In WebGL this directory is backed by Unity's virtual IndexedDB filesystem.
    public sealed class FileSaveStore
    {
        readonly string root;
        public FileSaveStore(string directory) { root = Path.GetFullPath(directory); }
        string FileName(string key) {
            using (var hash = SHA256.Create()) {
                var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Path.Combine(root, BitConverter.ToString(bytes).Replace("-", "") + ".bin");
            }
        }
        public void Write(string key, byte[] data) { Directory.CreateDirectory(root); File.WriteAllBytes(FileName(key),data); }
        public byte[] Read(string key) { string file=FileName(key); return File.Exists(file) ? File.ReadAllBytes(file) : null; }
        public void Delete(string key) { string file=FileName(key); if(File.Exists(file))File.Delete(file); }
        public void Clear() { if(Directory.Exists(root))foreach(string file in Directory.GetFiles(root,"*.bin"))File.Delete(file); }
    }
}
