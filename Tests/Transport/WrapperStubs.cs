using System;
using System.Collections.Generic;
using System.Text;
namespace UnityEngine { public static class Debug { public static void Log(object x) { Console.WriteLine(x); } public static void LogWarning(object x) { Console.WriteLine(x); } } }
namespace NroWeb { public class WebSocketBridge { public class WebSocketTransport : TestWire {} } }
namespace Game1 {
public interface ISession { void sendMessage(Message message); }
public interface IMessageHandler { void onMessage(Message message); void onConnectionFail(bool main); void onDisconnected(bool main); void onConnectOK(bool main); }
public class Message { public sbyte command; public bool? PacketInventory200, PacketItemOptionsInt32; sbyte[] data; public Message(sbyte command,sbyte[] data) { this.command=command;this.data=data; } public sbyte[] getData() {return data;} public myReader reader() {return new myReader(data);} }
public class myReader { byte[] data; int cursor; public myReader(sbyte[] bytes) {data=new byte[bytes.Length];Buffer.BlockCopy(bytes,0,data,0,bytes.Length);} public sbyte readByte(){return unchecked((sbyte)data[cursor++]);} public int readInt(){return (data[cursor++]<<24)|(data[cursor++]<<16)|(data[cursor++]<<8)|data[cursor++];} public string readUTF(){int n=(data[cursor++]<<8)|data[cursor++];string s=Encoding.UTF8.GetString(data,cursor,n);cursor+=n;return s;} }
public class MyVector { List<object> values=new List<object>(); public void removeAllElements(){values.Clear();} public void addElement(object x){values.Add(x);} public int size(){return values.Count;} public object elementAt(int i){return values[i];} public void removeElementAt(int i){values.RemoveAt(i);} }
public static class mSystem { public static long currentTimeMillis(){return DateTime.UtcNow.Ticks/10000;} }
public static class ServerListScreen { public static int testConnect; }
public static class Controller { public static bool isStopReadMessage; }
public static class GameMidlet { public static string IP2; public static int PORT2; public static bool isConnect2; }
public static class GameCanvas { public static int Connected2; public static void connect2(){Connected2++;} }
}
namespace Game2 {
public interface ISession { void sendMessage(Message message); }
public interface IMessageHandler { void onMessage(Message message); void onConnectionFail(bool main); void onDisconnected(bool main); void onConnectOK(bool main); }
public class Message { public sbyte command; public bool? PacketInventory200, PacketItemOptionsInt32; sbyte[] data; public Message(sbyte command,sbyte[] data) { this.command=command;this.data=data; } public sbyte[] getData() {return data;} public myReader reader() {return new myReader(data);} }
public class myReader { byte[] data; int cursor; public myReader(sbyte[] bytes) {data=new byte[bytes.Length];Buffer.BlockCopy(bytes,0,data,0,bytes.Length);} public sbyte readByte(){return unchecked((sbyte)data[cursor++]);} public int readInt(){return (data[cursor++]<<24)|(data[cursor++]<<16)|(data[cursor++]<<8)|data[cursor++];} public string readUTF(){int n=(data[cursor++]<<8)|data[cursor++];string s=Encoding.UTF8.GetString(data,cursor,n);cursor+=n;return s;} }
public class MyVector { List<object> values=new List<object>(); public void removeAllElements(){values.Clear();} public void addElement(object x){values.Add(x);} public int size(){return values.Count;} public object elementAt(int i){return values[i];} public void removeElementAt(int i){values.RemoveAt(i);} }
public static class mSystem { public static long currentTimeMillis(){return DateTime.UtcNow.Ticks/10000;} }
public static class ServerListScreen { public static int testConnect; }
public static class Controller { public static bool isStopReadMessage; }
public static class GameMidlet { public static string IP2; public static int PORT2; public static bool isConnect2; }
public static class GameCanvas { public static int Connected2; public static void connect2(){Connected2++;} }
}
