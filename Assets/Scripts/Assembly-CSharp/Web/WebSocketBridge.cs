using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace NroWeb
{
    // SendMessage targets a stable object; monotonically increasing IDs reject old socket callbacks.
    [Preserve]
    public sealed class WebSocketBridge : MonoBehaviour
    {
        const string ObjectName = "NroWebSocketBridge";
        static WebSocketBridge instance;
        static int nextId;
        static readonly Dictionary<int, WebSocketTransport> connections = new Dictionary<int, WebSocketTransport>();
        public static int Register(WebSocketTransport connection)
        {
            if (instance == null)
            {
                var obj = new GameObject(ObjectName); instance = obj.AddComponent<WebSocketBridge>();
                DontDestroyOnLoad(obj);
            }
            int id = ++nextId; connections.Add(id, connection); return id;
        }
        public static void Unregister(int id) { connections.Remove(id); }
        [Preserve]
        public void OnWebSocketOpened(string value)
        {
            int id; WebSocketTransport connection;
            if (int.TryParse(value, out id) && connections.TryGetValue(id, out connection)) connection.OnOpened();
        }
        [Preserve]
        public void OnWebSocketBytes(string value)
        {
            int separator = value.IndexOf('|'), id; WebSocketTransport connection;
            if (separator < 0 || !int.TryParse(value.Substring(0, separator), out id) || !connections.TryGetValue(id, out connection)) return;
            try { connection.OnBytes(Convert.FromBase64String(value.Substring(separator + 1))); }
            catch (Exception ex) { Debug.LogWarning("Websocket payload: " + ex.Message); connection.OnEnded(); }
        }
        [Preserve]
        public void OnWebSocketEnded(string value)
        {
            int id; WebSocketTransport connection;
            if (int.TryParse(value, out id) && connections.TryGetValue(id, out connection)) connection.OnEnded();
        }
        void OnDestroy()
        {
            if (instance != this) return;
            var all = new List<WebSocketTransport>(connections.Values);
            foreach (var connection in all) connection.OnEnded();
            connections.Clear(); instance = null;
        }
        public sealed class WebSocketTransport : IWireTransport
        {
            int id;
            Action opened, ended;
            Action<byte[]> bytes;
            [DllImport("__Internal")] static extern void NroWSOpen(int id, string receiver, string host, int port);
            [DllImport("__Internal")] static extern int NroWSSend(int id, byte[] data, int length);
            [DllImport("__Internal")] static extern void NroWSClose(int id);
            public void Open(string host, int port, Action onOpened, Action<byte[]> onBytes, Action onEnded)
            {
                Close(); opened = onOpened; bytes = onBytes; ended = onEnded;
#if UNITY_WEBGL && !UNITY_EDITOR
                id = Register(this); NroWSOpen(id, ObjectName, host, port);
#else
                throw new PlatformNotSupportedException("Browser websocket transport requires WebGL player");
#endif
            }
            public void Send(byte[] data)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (id == 0 || NroWSSend(id, data, data.Length) == 0) throw new InvalidOperationException("Websocket send failed");
#else
                throw new PlatformNotSupportedException("Browser websocket transport requires WebGL player");
#endif
            }
            public void Close()
            {
                int previous = id; id = 0;
                opened = ended = null; bytes = null;
                if (previous == 0) return;
                Unregister(previous);
#if UNITY_WEBGL && !UNITY_EDITOR
                NroWSClose(previous);
#endif
            }
            public void OnOpened() { if (opened != null) opened(); }
            public void OnBytes(byte[] data) { if (bytes != null) bytes(data); }
            public void OnEnded() { var callback = ended; Close(); if (callback != null) callback(); }
        }
    }
}
