#if !(UNITY_WEBGL && !UNITY_EDITOR) && !NRO_WEB_TRANSPORT_TEST
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Game2
{
	public class Session_ME : ISession
	{
		public class Sender
		{
			public List<Message> sendingMessage;

			public Sender()
			{
				sendingMessage = new List<Message>();
			}

			public void AddMessage(Message message)
			{
				sendingMessage.Add(message);
			}

			public void run()
			{
				while (connected)
				{
					try
					{
						if (getKeyComplete)
						{
							while (sendingMessage.Count > 0)
							{
								doSendMessage(sendingMessage[0]);
								sendingMessage.RemoveAt(0);
							}
						}
						try
						{
							Thread.Sleep(5);
						}
						catch (Exception ex)
						{
							Cout.LogError(ex.ToString());
						}
					}
					catch (Exception)
					{
						Res.outz("error send message! ");
					}
				}
			}
		}

		private class MessageCollector
		{
			public void run()
			{
				try
				{
					while (connected)
					{
						Message message = readMessage();
						if (message == null)
						{
							break;
						}
						try
						{
							if (message.command == -27)
							{
								getKey(message);
							}
							else
							{
								onRecieveMsg(message);
							}
						}
						catch (Exception)
						{
							Cout.println("LOI NHAN  MESS THU 1");
						}
						try
						{
							Thread.Sleep(5);
						}
						catch (Exception)
						{
							Cout.println("LOI NHAN  MESS THU 2");
						}
					}
				}
				catch (Exception ex3)
				{
					Debug.Log(ex3.Message.ToString());
				}
				if (!connected)
				{
					return;
				}
				if (messageHandler != null)
				{
					if (currentTimeMillis() - timeConnected > 500)
					{
						messageHandler.onDisconnected(isMainSession);
					}
					else
					{
						messageHandler.onConnectionFail(isMainSession);
					}
				}
				if (sc != null)
				{
					cleanNetwork();
				}
			}
	private void getKey(Message message)
			{
				try
				{
					sbyte b = message.reader().readSByte();
					key = new sbyte[b];
					for (int i = 0; i < b; i++)
					{
						key[i] = message.reader().readSByte();
					}
					for (int j = 0; j < key.Length - 1; j++)
					{
						key[j + 1] ^= key[j];
					}
					getKeyComplete = true;
					GameMidlet.IP2 = message.reader().readUTF();
					GameMidlet.PORT2 = message.reader().readInt();
					GameMidlet.isConnect2 = ((message.reader().readByte() != 0) ? true : false);
					if (isMainSession && GameMidlet.isConnect2)
					{
						GameCanvas.connect2();
					}
				}
				catch (Exception)
				{
				}
			}
			private int readLengthByte(bool extended)
			{
				sbyte value = dis.ReadSByte();
				if (getKeyComplete) value = readKey(value);
				return extended ? value + 128 : value & 255;
			}

			private Message readPayload(sbyte command, int length, int headerLength)
			{
				if (length < 0 || length > 0xFFFFFF) throw new InvalidDataException("Invalid message length");
				if (dis.BaseStream.CanSeek && length > dis.BaseStream.Length - dis.BaseStream.Position)
					throw new EndOfStreamException("Truncated message payload");
				byte[] bytes = dis.ReadBytes(length);
				if (bytes.Length != length) throw new EndOfStreamException("Truncated message payload");
				sbyte[] data = new sbyte[length];
				Buffer.BlockCopy(bytes, 0, data, 0, length);
				if (getKeyComplete)
				{
					for (int i = 0; i < data.Length; i++) data[i] = readKey(data[i]);
				}
				recvByteCount += headerLength + length;
				int total = recvByteCount + sendByteCount;
				strRecvByteCount = total / 1024 + "." + total % 1024 / 102 + "Kb";
				// Apply negotiation on this collector before it reads the next frame.
				// Inspect raw payload without advancing the main-thread message reader.
				if (command == -29 && data.Length >= 2 && data[0] == 100)
				{
					UseItemOptionsInt32 = data[1] != 0;
					UseInventory200 = data.Length >= 3 && data[2] != 0;
				}
				Message message = new Message(command, data);
				message.PacketItemOptionsInt32 = UseItemOptionsInt32;
				message.PacketInventory200 = UseInventory200;
				return message;
			}

			private Message readMessage2(sbyte command)
			{
				int low = readLengthByte(true);
				int middle = readLengthByte(true);
				int high = readLengthByte(true);
				return readPayload(command, low | (middle << 8) | (high << 16), 4);
			}

			private Message readMessage()
			{
				try
				{
					sbyte command = dis.ReadSByte();
					if (getKeyComplete) command = readKey(command);
					if (command == -32 || command == -66 || command == 11 || command == -67 || command == -74 || command == -87 || command == 66 || command == 12  || (UseInventory200 && (command == -30 || command == -35 || command == -36 || command == -86 || command == -44)))
						return readMessage2(command);
					int high = readLengthByte(false);
					int low = readLengthByte(false);
					return readPayload(command, (high << 8) | low, 3);
				}
				catch (Exception)
				{
					return null;
				}
			}
		}

		protected static Session_ME instance = new Session_ME();

		private static NetworkStream dataStream;

		private static BinaryReader dis;

		private static BinaryWriter dos;

		public static IMessageHandler messageHandler;

		public static bool isMainSession = true;

		private static TcpClient sc;

		public static bool connected;

		public static bool connecting;

		private static Sender sender = new Sender();

		public static Thread initThread;

		public static Thread collectorThread;

		public static Thread sendThread;

		public static int sendByteCount;

		public static int recvByteCount;

		public static bool UseInventory200;

		public static bool UseItemOptionsInt32;

		private static bool getKeyComplete;

		public static sbyte[] key = null;

		private static sbyte curR;

		private static sbyte curW;

		private static int timeConnected;

		public static string strRecvByteCount = string.Empty;

		public static bool isCancel;

		private string host;

		private int port;

		private long timeWaitConnect;

		public static int count;

		public static MyVector recieveMsg = new MyVector();

		public void clearSendingMessage()
		{
			sender.sendingMessage.Clear();
		}

		public static Session_ME gI()
		{
			if (instance == null)
			{
				instance = new Session_ME();
			}
			return instance;
		}

		public bool isConnected()
		{
			if (connected && sc != null)
			{
				return dis != null;
			}
			return false;
		}

		public void setHandler(IMessageHandler msgHandler)
		{
			messageHandler = msgHandler;
		}

		public void connect(string host, int port)
		{
			if (!connected && !connecting && mSystem.currentTimeMillis() >= timeWaitConnect)
			{
				timeWaitConnect = mSystem.currentTimeMillis() + 50;
				if (isMainSession)
				{
					ServerListScreen.testConnect = -1;
				}
				this.host = host;
				this.port = port;
				getKeyComplete = false;
				close();
				Debug.Log("host: " + host + ":" + port);
				initThread = new Thread(NetworkInit);
				initThread.Start();
			}
		}

		private async void NetworkInit()
		{
			isCancel = false;
			connecting = true;
			Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;
			connected = true;
			try
			{
				doConnect(host, port);
				messageHandler.onConnectOK(isMainSession);
			}
			catch (Exception)
			{
				if (messageHandler != null)
				{
					close();
					messageHandler.onConnectionFail(isMainSession);
				}
			}
		}

		public void doConnect(string host, int port)
		{
			sc = new TcpClient();
			sc.Connect(host, port);
			dataStream = sc.GetStream();
			dis = new BinaryReader(dataStream, new UTF8Encoding());
			dos = new BinaryWriter(dataStream, new UTF8Encoding());
			sendThread = new Thread(sender.run);
			sendThread.Start();
			collectorThread = new Thread(new MessageCollector().run);
			collectorThread.Start();
			timeConnected = currentTimeMillis();
			connecting = false;
			doSendMessage(new Message(-27));
			key = null;
		}

		public void sendMessage(Message message)
		{
			count++;
			Res.outz("SEND MSG: " + message.command);
			sender.AddMessage(message);
		}

		private static void doSendMessage(Message m)
		{
			sbyte[] data = m.getData();
			try
			{
				if (getKeyComplete)
				{
					sbyte value = writeKey(m.command);
					dos.Write(value);
				}
				else
				{
					dos.Write(m.command);
				}
				if (data != null)
				{
					int num = data.Length;
					if (getKeyComplete)
					{
						int num2 = writeKey((sbyte)(num >> 8));
						dos.Write((sbyte)num2);
						int num3 = writeKey((sbyte)(num & 0xFF));
						dos.Write((sbyte)num3);
					}
					else
					{
						dos.Write((ushort)num);
					}
					if (getKeyComplete)
					{
						for (int i = 0; i < data.Length; i++)
						{
							sbyte value2 = writeKey(data[i]);
							dos.Write(value2);
						}
					}
					sendByteCount += 5 + data.Length;
				}
				else
				{
					if (getKeyComplete)
					{
						int num4 = writeKey(0);
						dos.Write((sbyte)num4);
						int num5 = writeKey(0);
						dos.Write((sbyte)num5);
					}
					else
					{
						dos.Write((ushort)0);
					}
					sendByteCount += 5;
				}
				dos.Flush();
			}
			catch (Exception ex)
			{
				Debug.Log(ex.StackTrace);
				dos.Flush();
			}
		}

		public static sbyte readKey(sbyte b)
		{
			sbyte result = (sbyte)((key[curR++] & 0xFF) ^ (b & 0xFF));
			if (curR >= key.Length)
			{
				curR %= (sbyte)key.Length;
			}
			return result;
		}

		public static sbyte writeKey(sbyte b)
		{
			sbyte result = (sbyte)((key[curW++] & 0xFF) ^ (b & 0xFF));
			if (curW >= key.Length)
			{
				curW %= (sbyte)key.Length;
			}
			return result;
		}

		private const int MaxReceiveMessagesPerFrame = 64;
		private const int ReceiveBudgetMilliseconds = 2;
		private static int lastReceiveFrame = -1;

		public static void onRecieveMsg(Message msg)
		{
			// Main-thread arrivals must obey the same FIFO and capsule pause.
			lock (recieveMsg)
			{
				recieveMsg.addElement(msg);
			}
		}

		public static void update()
		{
			int frame = Time.frameCount;
			if (Controller.isStopReadMessage || lastReceiveFrame == frame) return;
			lock (recieveMsg)
			{
				if (recieveMsg.size() == 0) return;
			}
			lastReceiveFrame = frame;
			long started = System.Diagnostics.Stopwatch.GetTimestamp();
			for (int processed = 0; processed < MaxReceiveMessagesPerFrame; processed++)
			{
				// A departure handler can suspend reads until Teleport finishes.
				if (Controller.isStopReadMessage) return;
				Message message;
				lock (recieveMsg)
				{
					if (recieveMsg.size() == 0) return;
					message = (Message)recieveMsg.elementAt(0);
					recieveMsg.removeElementAt(0);
				}
				// Never hold the producer lock while loading map/image resources.
				if (message != null) messageHandler.onMessage(message);
				if ((System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000
					>= System.Diagnostics.Stopwatch.Frequency * ReceiveBudgetMilliseconds) return;
			}
		}

		public void close()
		{
			cleanNetwork();
		}

		private static void cleanNetwork()
		{
			UseInventory200 = false;
			UseItemOptionsInt32 = false;
			ItemOptionWire.UseInt32 = false;
			key = null;
			curR = 0;
			curW = 0;
			try
			{
				connected = false;
				connecting = false;
				if (sc != null)
				{
					sc.Close();
					sc = null;
				}
				if (dataStream != null)
				{
					dataStream.Close();
					dataStream = null;
				}
				if (dos != null)
				{
					dos.Close();
					dos = null;
				}
				if (dis != null)
				{
					dis.Close();
					dis = null;
				}
				if (Thread.CurrentThread.Name == Main.mainThreadName)
				{
					if (sendThread != null)
					{
						sendThread.Abort();
					}
					sendThread = null;
					if (initThread != null)
					{
						initThread.Abort();
					}
					initThread = null;
					if (collectorThread != null)
					{
						collectorThread.Abort();
					}
					collectorThread = null;
				}
				else
				{
					sendThread = null;
					initThread = null;
					collectorThread = null;
				}
				if (isMainSession)
				{
					ServerListScreen.testConnect = 0;
				}
			}
			catch (Exception)
			{
			}
		}

		public static int currentTimeMillis()
		{
			return Environment.TickCount;
		}

		public bool isCompareIPConnect()
		{
			return true;
		}
	}
}

#else
using System;
using System.Collections.Generic;
using NroWeb;
using UnityEngine;

namespace Game2
{
    public class Session_ME : ISession
    {
        protected static Session_ME instance = new Session_ME();
        public static IMessageHandler messageHandler;
        public static bool isMainSession = true;
        public static bool connected, connecting, UseInventory200, UseItemOptionsInt32, isCancel;
        public static sbyte[] key;
        public static int sendByteCount, recvByteCount, count;
        public static string strRecvByteCount = string.Empty;
        public static MyVector recieveMsg = new MyVector();
        const int MaximumQueuedPackets = 4096, MaximumQueuedBytes = 8 * 1024 * 1024;
        const int MaxReceiveMessagesPerFrame = 64, ReceiveBudgetMilliseconds = 2;
        static readonly Dictionary<Message, int> receiveSizes = new Dictionary<Message, int>();
        static int queuedReceiveBytes;
        static int lastReceiveFrame = -1;
        static Action deferredEnded;
        readonly WebSessionCore core;
        long timeWaitConnect;
        int timeConnected;
        public Session_ME() : this(new WebSocketBridge.WebSocketTransport()) { }
        public Session_ME(IWireTransport wire)
        {
            core = new WebSessionCore(true, wire);
            core.Changed = Synchronize;
            core.Error = delegate(Exception ex) { Debug.LogWarning("Browser session: " + ex.Message); };
            core.Opened = delegate
            {
                timeConnected = currentTimeMillis();
                if (messageHandler != null) messageHandler.onConnectOK(isMainSession);
            };
            core.Ended = delegate(bool wasConnected)
            {
                // TCP EOF may follow a complete packet in the same browser event turn.
                // Drain decoded frames before reporting disconnection to the controller.
                bool disconnected = wasConnected && unchecked(currentTimeMillis() - timeConnected) > 500;
                if (recieveMsg.size() == 0) NotifyEnded(disconnected);
                else deferredEnded = delegate { NotifyEnded(disconnected); };
            };
            core.Handshake = ReadHandshake;
            core.Packet = delegate(ProtocolPacket packet) { QueueMessage(ToMessage(packet), packet.Data.Length + 4); };
        }
        void NotifyEnded(bool disconnected)
        {
            if (isMainSession) ServerListScreen.testConnect = 0;
            if (messageHandler == null) return;
            if (disconnected) messageHandler.onDisconnected(isMainSession);
            else messageHandler.onConnectionFail(isMainSession);
        }
        static void ClearReceiveQueue()
        {
            lock (recieveMsg)
            {
                recieveMsg.removeAllElements(); receiveSizes.Clear(); queuedReceiveBytes = 0; deferredEnded = null;
            }
        }
        static void QueueMessage(Message message, int bytes)
        {
            lock (recieveMsg)
            {
                if (recieveMsg.size() >= MaximumQueuedPackets || bytes > MaximumQueuedBytes - queuedReceiveBytes)
                    throw new InvalidOperationException("Decoded protocol queue exceeded");
                receiveSizes.Add(message, bytes); queuedReceiveBytes += bytes; recieveMsg.addElement(message);
            }
        }
        static Message ToMessage(ProtocolPacket packet)
        {
            return new Message(packet.Command, packet.Data)
            {
                PacketItemOptionsInt32 = packet.ItemOptionsInt32,
                PacketInventory200 = packet.Inventory200
            };
        }
        void ReadHandshake(ProtocolPacket packet)
        {
            int length = packet.Data[0]; key = new sbyte[length];
            for (int i = 0; i < length; i++) key[i] = packet.Data[i + 1];
            for (int i = 1; i < length; i++) key[i] ^= key[i - 1];
            // Some servers send only the key. Secondary routing metadata is optional.
            if (packet.Data.Length == length + 1) return;
            var reader = ToMessage(packet).reader();
            reader.readByte(); for (int i = 0; i < length; i++) reader.readByte();
            GameMidlet.IP2 = reader.readUTF(); GameMidlet.PORT2 = reader.readInt();
            GameMidlet.isConnect2 = reader.readByte() != 0;
            if (isMainSession && GameMidlet.isConnect2) GameCanvas.connect2();
        }
        void Synchronize()
        {
            connected = core.Connected; connecting = core.Connecting;
            UseItemOptionsInt32 = core.Codec.ItemOptionsInt32; UseInventory200 = core.Codec.Inventory200;
            sendByteCount = core.Codec.SentBytes; recvByteCount = core.Codec.ReceivedBytes;
            int total = sendByteCount + recvByteCount;
            strRecvByteCount = total / 1024 + "." + total % 1024 / 102 + "Kb";
            if (!core.Codec.HasKey) key = null;
        }
        public static Session_ME gI() { if (instance == null) instance = new Session_ME(); return instance; }
        public bool isConnected() { return core.Connected; }
        public void setHandler(IMessageHandler handler) { messageHandler = handler; }
        public void connect(string host, int port)
        {
            if (core.Connected || core.Connecting || mSystem.currentTimeMillis() < timeWaitConnect) return;
            timeWaitConnect = mSystem.currentTimeMillis() + 50;
            if (isMainSession) ServerListScreen.testConnect = -1;
            ClearReceiveQueue(); isCancel = false; core.Connect(host, port);
        }
        public void doConnect(string host, int port) { connect(host, port); }
        public void sendMessage(Message message) { count++; core.Send(message.command, message.getData()); }
        public void clearSendingMessage() { core.ClearSending(); }
        public void close()
        {
            isCancel = true; core.Close(); ClearReceiveQueue();
            if (isMainSession) ServerListScreen.testConnect = 0;
        }
        public static sbyte readKey(sbyte value) { return gI().core.Codec.ReadKey(value); }
        public static sbyte writeKey(sbyte value) { return gI().core.Codec.WriteKey(value); }
        public static void onRecieveMsg(Message message) { QueueMessage(message, 4); }
        public static void update()
        {
            int frame = Time.frameCount;
            Action callback = null;
            lock (recieveMsg)
            {
                if (recieveMsg.size() == 0 && deferredEnded != null)
                {
                    callback = deferredEnded;
                    deferredEnded = null;
                }
            }
            if (callback != null) { callback(); return; }
            if (Controller.isStopReadMessage || messageHandler == null || lastReceiveFrame == frame) return;
            lastReceiveFrame = frame;
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            for (int processed = 0; processed < MaxReceiveMessagesPerFrame; processed++)
            {
                if (Controller.isStopReadMessage || messageHandler == null) return;
                Message message;
                lock (recieveMsg)
                {
                    if (recieveMsg.size() == 0) break;
                    message = (Message)recieveMsg.elementAt(0);
                    recieveMsg.removeElementAt(0);
                    int bytes;
                    if (message != null && receiveSizes.TryGetValue(message, out bytes))
                    {
                        queuedReceiveBytes -= bytes;
                        receiveSizes.Remove(message);
                    }
                }
                if (message != null) messageHandler.onMessage(message);
                if ((System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000
                    >= System.Diagnostics.Stopwatch.Frequency * ReceiveBudgetMilliseconds) return;
            }
            callback = null;
            lock (recieveMsg)
            {
                if (recieveMsg.size() == 0 && deferredEnded != null)
                {
                    callback = deferredEnded;
                    deferredEnded = null;
                }
            }
            if (callback != null) callback();
        }
        public static int currentTimeMillis() { return Environment.TickCount; }
        public bool isCompareIPConnect() { return true; }
    }
}
#endif
