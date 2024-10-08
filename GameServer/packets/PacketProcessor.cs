﻿using System.Collections;
using System.Net.Sockets;
using System.Reflection;
using System.Timers;
using log4net;
using Project.GS.ServerProperties;
using Project.Network;
using Timer=System.Timers.Timer;

namespace Project.GS.PacketHandler;

/// <summary>
/// This class handles the packets, receiving and sending
/// </summary>
public class PacketProcessor
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Sync Lock Object
	/// </summary>
	private readonly object m_SyncLock = new object();
	
	/// <summary>
	/// Holds the current client for this processor
	/// </summary>
	protected readonly GameClient m_client;

	/// <summary>
	/// Holds the encoding used to encrypt/decrypt the packets
	/// </summary>
	protected readonly IPacketEncoding m_encoding;

	/// <summary>
	/// Stores all packet handlers found when searching the gameserver assembly
	/// </summary>
	protected IPacketHandler[] m_packetHandlers = new IPacketHandler[256];

	/// <summary>
	/// currently active packet handler
	/// </summary>
	protected IPacketHandler m_activePacketHandler;

	/// <summary>
	/// thread id of running packet handler
	/// </summary>
	protected int m_handlerThreadID;

    /// <summary>
    /// packet preprocessor that performs initial packet checks for this Packet Processor.
    /// </summary>
    protected PacketPreprocessing m_packetPreprocessor;

    /// <summary>
    /// Constructs a new PacketProcessor
    /// </summary>
    /// <param name="client">The processor client</param>
    public PacketProcessor(GameClient client)
	{
		if (client == null)
			throw new ArgumentNullException("client");
		m_client = client;
        m_packetPreprocessor = new PacketPreprocessing();

        LoadPacketHandlers(client);
        
        //TODO set encoding based on client version in the future :)
		// if (client.Version < GameClient.eClientVersion.Version1110)
		// 	m_encoding = new PacketEncoding168();
		// else
		// 	m_encoding = new PacketEncoding1110();
		m_tcpSendBuffer = client.Server.AcquirePacketBuffer();

	}

	#region Last Packets

	/// <summary>
	/// The count of last sent/received packets to keep.
	/// </summary>
	protected const int MAX_LAST_PACKETS = 16;

	/// <summary>
	/// Holds the last sent/received packets.
	/// </summary>
	protected readonly Queue<IPacket> m_lastPackets = new Queue<IPacket>(MAX_LAST_PACKETS);

	/// <summary>
	/// Saves the sent packet for debugging
	/// </summary>
	/// <param name="pak">The sent packet</param>
	protected void SavePacket(IPacket pak)
	{
		lock (((ICollection)m_lastPackets).SyncRoot)
		{
			while (m_lastPackets.Count >= MAX_LAST_PACKETS)
				m_lastPackets.Dequeue();

			m_lastPackets.Enqueue(pak);
		}
	}

	/// <summary>
	/// Makes a copy of last sent/received packets.
	/// </summary>
	/// <returns></returns>
	public IPacket[] GetLastPackets()
	{
		lock (((ICollection)m_lastPackets).SyncRoot)
		{
			return m_lastPackets.ToArray();
		}
	}

	#endregion

	/// <summary>
	/// Gets the encoding for this processor
	/// </summary>
	public IPacketEncoding Encoding
	{
		get { return m_encoding; }
	}

    /// <summary>
    /// Caches packet handlers loaded for a given client version (in string format, used for namespace search).
    /// </summary>
    private static Dictionary<string, IPacketHandler[]> m_cachedPacketHandlerSearchResults = new Dictionary<string, IPacketHandler[]>();
    /// <summary>
    /// Stores packet handler attributes for each version, required to load preprocessors.
    /// </summary>
    private static Dictionary<string, List<PacketHandlerAttribute>> m_cachedPreprocessorSearchResults = new Dictionary<string, List<PacketHandlerAttribute>>();
    private static object m_packetHandlerCacheLock = new object();

    public virtual void LoadPacketHandlers(GameClient client)
    {
        string baseVersion = "v168";
        //String may seem cumbersome but I would like to leave the open of custom clients open without core modification (for this reason I cannot use eClientVersion).
        //Also I am merely reusing some already written search functionality, which searches a namespace and thus expects a string.

        List<PacketHandlerAttribute> attributes = new List<PacketHandlerAttribute>();
        LoadPacketHandlers(baseVersion, out m_packetHandlers, out attributes);

        //todo: load different handlers for cumulative client versions, overwriting duplicate entries in m_PacketHandlers with later version.

        //Add preprocessors for each packet handler
        foreach (PacketHandlerAttribute pha in attributes)
        {
            m_packetPreprocessor.RegisterPacketDefinition(pha.Code, pha.PreprocessorID);
        }
    }

    /// <summary>
    /// Loads packet handlers to be used for handling incoming data from this game client.
    /// </summary>
    /// <param name="client"></param>
    public virtual void LoadPacketHandlers(string version, out IPacketHandler[] packetHandlers, out List<PacketHandlerAttribute> attributes)
    {
        packetHandlers = new IPacketHandler[256];
        attributes = new List<PacketHandlerAttribute>();

        Array.Clear(packetHandlers, 0, packetHandlers.Length);
        lock (m_packetHandlerCacheLock)
        {
            if (!m_cachedPacketHandlerSearchResults.ContainsKey(version))
            {
                int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), packetHandlers);
                if (log.IsInfoEnabled)
                    log.Info("PacketProcessor: Loaded " + count + " handlers from GameServer Assembly!");

                count = 0;
                foreach (Assembly asm in ScriptMgr.Scripts)
                {
                    count += SearchAndAddPacketHandlers(version, asm, packetHandlers);
                }
                if (log.IsInfoEnabled)
                    log.Info("PacketProcessor: Loaded " + count + " handlers from Script Assemblys!");

                //save search result for next login
                m_cachedPacketHandlerSearchResults.Add(version, (IPacketHandler[])packetHandlers.Clone());
            }
            else
            {
                packetHandlers = (IPacketHandler[])m_cachedPacketHandlerSearchResults[version].Clone();
                int count = 0;
                foreach (IPacketHandler ph in packetHandlers) if (ph != null) count++;
                log.Info("PacketProcessor: Loaded " + count + " handlers from cache for version="+version+"!");
            }

            if (m_cachedPreprocessorSearchResults.ContainsKey(version))
                attributes = m_cachedPreprocessorSearchResults[version];
            log.Info("PacketProcessor: Loaded " + attributes.Count + " preprocessors from cache for version=" + version + "!");
        }
    }

	/// <summary>
	/// Registers a packet handler
	/// </summary>
	/// <param name="handler">The packet handler to register</param>
	/// <param name="packetCode">The packet ID to register it with</param>
	public void RegisterPacketHandler(int packetCode, IPacketHandler handler, IPacketHandler[] packetHandlers)
	{
		if (packetHandlers[packetCode] != null)
		{
			log.InfoFormat("Overwriting Client Packet Code {0}, with handler {1} in PacketProcessor", packetCode, handler.GetType().FullName);
		}

        packetHandlers[packetCode] = handler;
	}



	/// <summary>
	/// Searches an assembly for packet handlers
	/// </summary>
	/// <param name="version">namespace of packethandlers to search eg. 'v167'</param>
	/// <param name="assembly">Assembly to search</param>
	/// <returns>The number of handlers loaded</returns>
	protected int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
	{
		int count = 0;

		// Walk through each type in the assembly
		foreach (Type type in assembly.GetTypes())
		{
			// Pick up a class
			if (type.IsClass != true)
				continue;

			if (type.GetInterface("DOL.GS.PacketHandler.IPacketHandler") == null)
				continue;

			if (!type.Namespace.ToLower().EndsWith(version.ToLower()))
				continue;

			var packethandlerattribs =
				(PacketHandlerAttribute[]) type.GetCustomAttributes(typeof (PacketHandlerAttribute), true);
			if (packethandlerattribs.Length > 0)
			{
				count++;
				RegisterPacketHandler(packethandlerattribs[0].Code, (IPacketHandler) Activator.CreateInstance(type), packetHandlers);

                if (!m_cachedPreprocessorSearchResults.ContainsKey(version)) m_cachedPreprocessorSearchResults.Add(version, new List<PacketHandlerAttribute>());
                m_cachedPreprocessorSearchResults[version].Add(packethandlerattribs[0]);
            }
		}
		return count;
	}

	/// <summary>
	/// Called on client disconnect.
	/// </summary>
	public virtual void OnDisconnect()
	{
		byte[] tcp = m_tcpSendBuffer;
		m_tcpSendBuffer = null;
		m_client.Server.ReleasePacketBuffer(tcp);
	}

	#region TCP

	/// <summary>
	/// Holds the TCP send buffer
	/// </summary>
	protected byte[] m_tcpSendBuffer;

	/// <summary>
	/// The client TCP packet send queue
	/// </summary>
	protected readonly Queue<byte[]> m_tcpQueue = new Queue<byte[]>(256);

	/// <summary>
	/// Indicates whether data is currently being sent to the client
	/// </summary>
	protected bool m_sendingTcp;

	/// <summary>
	/// Sends a packet via TCP
	/// </summary>
	/// <param name="packet">The packet to be sent</param>
	public void SendTCP(GSTCPPacketOut packet)
	{
		//Fix the packet size
		packet.WritePacketLength();

		SavePacket(packet);

		//Get the packet buffer
		byte[] buf = packet.GetBuffer(); //packet.WritePacketLength sets the Capacity

		//Send the buffer
		SendTCP(buf);
	}

	/// <summary>
	/// Sends a packet via TCP
	/// </summary>
	/// <param name="buf">Buffer containing the data to be sent</param>
	public void SendTCP(byte[] buf)
	{
		if (m_tcpSendBuffer == null)
			return;

		//Check if client is connected
		if (m_client.Socket.Connected)
		{
			if (log.IsDebugEnabled)
				log.Debug(Marshal.ToHexDump(
					        string.Format("<=== <{2}> Packet 0x{0:X2} (0x{1:X2}) length: {3}", buf[2], buf[2] ^ 168,
					                      (m_client.Account != null) ? m_client.Account.Name : m_client.TcpEndpoint, buf.Length),
					        buf));

			if (buf.Length > 2048)
			{
				if (log.IsErrorEnabled)
				{
					string desc =
						String.Format(
							"Sending packets longer than 2048 cause client to crash, check Log for stacktrace. Packet code: 0x{0:X2}, account: {1}, packet size: {2}.",
							buf[2], (m_client.Account != null) ? m_client.Account.Name : m_client.TcpEndpoint, buf.Length);
					log.Error(Marshal.ToHexDump(desc, buf) + "\n" + Environment.StackTrace);

					if (Properties.IGNORE_TOO_LONG_OUTCOMING_PACKET)
					{
						log.Error("ALERT: Oversize packet detected and discarded.");
						//m_client.Out.SendMessage("ALERT: Error sending an update to your client. Oversize packet detected and discarded. Please /report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
					}
					else
					{
						GameServer.Instance.Disconnect(m_client);
					}
					return;
				}
			}

			m_encoding.EncryptPacket(buf, 0, false);

			try
			{
				var packetLength = (buf[0] << 8) + buf[1] + 3;
				Statistics.BytesOut += packetLength;
				Statistics.PacketsOut++;

				lock (((ICollection)m_tcpQueue).SyncRoot)
				{
					if (m_sendingTcp)
					{
						m_tcpQueue.Enqueue(buf);
						return;
					}
					
					m_sendingTcp = true;
				}

				Buffer.BlockCopy(buf, 0, m_tcpSendBuffer, 0, packetLength);

				int start = Environment.TickCount;

				m_client.Socket.BeginSend(m_tcpSendBuffer, 0, packetLength, SocketFlags.None, m_asyncTcpCallback, m_client);

				int took = Environment.TickCount - start;
				if (took > 100 && log.IsWarnEnabled)
					log.WarnFormat("SendTCP.BeginSend took {0}ms! (TCP to client: {1})", took, m_client);
			}
			catch (Exception e)
			{
				// assure that no exception is thrown into the upper layers and interrupt game loops!
				if (log.IsWarnEnabled)
					log.Warn("It seems <" + ((m_client.Account != null) ? m_client.Account.Name : "???") +
					         "> went linkdead. Closing connection. (SendTCP, " + e.GetType() + ": " + e.Message + ")");
				//DOLConsole.WriteWarning(e.ToString());
				GameServer.Instance.Disconnect(m_client);
			}
		}
	}

	/// <summary>
	/// Holds the TCP AsyncCallback delegate
	/// </summary>
	protected static readonly AsyncCallback m_asyncTcpCallback = AsyncTcpSendCallback;

	/// <summary>
	/// Callback method for async sends
	/// </summary>
	/// <param name="ar"></param>
	protected static void AsyncTcpSendCallback(IAsyncResult ar)
	{
		if (ar == null)
		{
			if (log.IsErrorEnabled)
				log.Error("AsyncSendCallback: ar == null");
			return;
		}

		var client = (GameClient) ar.AsyncState;

		try
		{
			PacketProcessor pakProc = client.PacketProcessor;
			Queue<byte[]> q = pakProc.m_tcpQueue;

            int sent = 0;
            if (client.IsConnected)
                sent = client.Socket.EndSend(ar);

			int dataLength = 0;
			byte[] data = pakProc.m_tcpSendBuffer;

			if (data == null)
				return;

			lock (((ICollection)q).SyncRoot)
			{
				if (q.Count > 0)
				{
					// log.WarnFormat("async sent {0} bytes, sending queued packets count: {1}", sent, q.Count);
					dataLength = CombineTCPPackets(data, q);
				}
				if (dataLength <= 0)
				{
					// log.WarnFormat("async sent {0} bytes", sent);
					pakProc.m_sendingTcp = false;
					return;
				}
			}

			int start = Environment.TickCount;

			client.Socket.BeginSend(data, 0, dataLength, SocketFlags.None, m_asyncTcpCallback, client);

			int took = Environment.TickCount - start;
			if (took > 100 && log.IsWarnEnabled)
				log.WarnFormat("AsyncTcpSendCallback.BeginSend took {0}ms! (TCP to client: {1})", took, client.ToString());
		}
		catch (ObjectDisposedException ex)
		{
			log.Error("Packet processor: ObjectDisposedException", ex);
			GameServer.Instance.Disconnect(client);
		}
		catch (SocketException ex)
		{
			log.Error("Packet processor: SocketException", ex);
			GameServer.Instance.Disconnect(client);
		}
		catch (Exception ex)
		{
			// assure that no exception is thrown into the upper layers and interrupt game loops!
			if (log.IsErrorEnabled)
				log.Error("AsyncSendCallback. client: " + client, ex);
			GameServer.Instance.Disconnect(client);
		}
	}

	/// <summary>
	/// Combines queued TCP packets in one stream.
	/// </summary>
	/// <param name="buf">The target buffer.</param>
	/// <param name="queue">The queued packets.</param>
	/// <returns>The count of bytes written.</returns>
	private static int CombineTCPPackets(byte[] buf, Queue<byte[]> queue)
	{
		var (buffer, length) = CombinePacket(3, buf, queue);
		// should never happen, the TCP buffer is way bigger than a single Daoc packet (~2kb max)
		if (buffer != buf)
			throw new Exception($"packet size {length} > buffer size {buf.Length}");
		return length;
	}

	/// <summary>
	/// Combines queued UDP packets in one stream.
	/// </summary>
	/// <param name="buf">The target buffer.</param>
	/// <param name="queue">The queued packets.</param>
	/// <returns>The buffer and count of bytes written.</returns>
	private static (byte[] buffer, int bufferLength) CombineUDPPackets(byte[] buf, Queue<byte[]> queue)
	{
		return CombinePacket(5, buf, queue);
	}

	private static (byte[] buffer, int bufferLength) CombinePacket(int headerSize, byte[] buffer, Queue<byte[]> packetQueue)
	{
		if (packetQueue.Count == 0)
			return (buffer, 0);

		var pak = packetQueue.Dequeue();
		var packetLength = (pak[0] << 8) + pak[1] + headerSize;

		if (packetLength > buffer.Length)
			return (pak, packetLength);
		Buffer.BlockCopy(pak, 0, buffer, 0, packetLength);

		int i = packetLength;
		while (packetQueue.Count > 0 && i + headerSize < buffer.Length)
		{
			pak = packetQueue.Peek();
			packetLength = (pak[0] << 8) + pak[1] + headerSize;

			if (i + packetLength > buffer.Length)
				break;

			Buffer.BlockCopy(pak, 0, buffer, i, packetLength);
			i += packetLength;
			packetQueue.Dequeue();
		}
		return (buffer, i);
	}

	/// <summary>
	/// Send the packet via TCP without changing any portion of the packet
	/// </summary>
	/// <param name="packet">Packet to send</param>
	public void SendTCPRaw(GSTCPPacketOut packet)
	{
		SendTCP((byte[]) packet.GetBuffer().Clone());
	}

	#endregion
	

	/// <summary>
	/// Called when the client receives bytes
	/// </summary>
	/// <param name="numBytes">The number of bytes received</param>
	public void ReceiveBytes(int numBytes)
	{
		lock (m_SyncLock)
		{
			byte[] buffer = m_client.ReceiveBuffer;

			//End Offset of buffer
			int bufferSize = m_client.ReceiveBufferOffset + numBytes;

			//Size < minimum
			if (bufferSize < GSPacketIn.HDR_SIZE)
			{
				m_client.ReceiveBufferOffset = bufferSize; // undo buffer read
				return;
			}

			//Reset the offset
			m_client.ReceiveBufferOffset = 0;

			//Current offset into the buffer
			int curOffset = 0;

			do
			{
				int packetLength = (buffer[curOffset] << 8) + buffer[curOffset + 1] + GSPacketIn.HDR_SIZE;
				int dataLeft = bufferSize - curOffset;

				if (dataLeft < packetLength)
				{
					Buffer.BlockCopy(buffer, curOffset, buffer, 0, dataLeft);
					m_client.ReceiveBufferOffset = dataLeft;
					break;
				}

				m_encoding.DecryptPacket(buffer, curOffset, false); // decrypt inplace

				int packetEnd = curOffset + packetLength;

				int calcCheck = CalculateChecksum(buffer, curOffset, packetLength - 2);
				int pakCheck = (buffer[packetEnd - 2] << 8) | (buffer[packetEnd - 1]);

				if (pakCheck != calcCheck)
				{
					if (log.IsWarnEnabled)
						log.WarnFormat(
							"Bad TCP packet checksum (packet:0x{0:X4} calculated:0x{1:X4}) -> disconnecting\nclient: {2}\ncurOffset={3}; packetLength={4}",
							pakCheck, calcCheck, m_client.ToString(), curOffset, packetLength);

					if (log.IsInfoEnabled)
					{
						log.Info("Last client sent/received packets (from older to newer):");

						foreach (IPacket prevPak in GetLastPackets())
						{
							log.Info(prevPak.ToHumanReadable());
						}
						
						log.Info(Marshal.ToHexDump("Last Received Bytes : ", buffer));
					}

					m_client.Disconnect();
					return;
				}

				var pak = new GSPacketIn(packetLength - GSPacketIn.HDR_SIZE);
				pak.Load(buffer, curOffset, packetLength);

				try
				{
					HandlePacket(pak);
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error("HandlePacket(pak)", e);
				}

				curOffset += packetLength;
			} while (bufferSize - 1 > curOffset);

			if (bufferSize - 1 == curOffset)
			{
				buffer[0] = buffer[curOffset];
				m_client.ReceiveBufferOffset = 1;
			}
		}
	}

	/// <summary>
	/// Calculates the packet checksum
	/// </summary>
	/// <param name="packet">The full received packet including checksum bytes</param>
	/// <param name="dataOffset">The offset of data for checksum calc in the buffer</param>
	/// <param name="dataSize">The size of data for checksum calc</param>
	/// <returns>The calculated checksum</returns>
	public static ushort CalculateChecksum(byte[] packet, int dataOffset, int dataSize)
	{
		byte[] pak = packet;
		byte val1 = 0x7E;
		byte val2 = 0x7E;
		int i = dataOffset;
		int len = i + dataSize;

		while (i < len)
		{
			val1 += pak[i++];
			val2 += val1;
		}

		return (ushort) (val2 - ((val1 + val2) << 8));
	}

	public void HandlePacketTimeout(object sender, ElapsedEventArgs e)
	{
		string source = ((m_client.Account != null) ? m_client.Account.Name : m_client.TcpEndpoint);
		if (log.IsErrorEnabled)
			log.Error("Thread " + m_handlerThreadID + " - Handler " + m_activePacketHandler.GetType() +
			          " takes too much time (>10000ms) <" + source + "> " + "!");
	}

#if LOGACTIVESTACKS
	/// <summary>
	/// Holds a list of all currently active handler threads!
	/// This list is updated in the HandlePacket method
	/// </summary>
	public static Hashtable m_activePacketThreads = Hashtable.Synchronized(new Hashtable());
#endif

	/// <summary>
	/// Retrieves a textual description of all active packet handler thread stacks
	/// </summary>
	/// <returns>A string with the stacks</returns>
	public static string GetConnectionThreadpoolStacks()
	{
#if LOGACTIVESTACKS
		var builder = new StringBuilder();
		//When enumerating over a synchronized hashtable, we need to
		//lock it's syncroot! Only for reading, not for writing locking
		//is needed!
		lock (m_activePacketThreads.SyncRoot)
		{
			foreach (DictionaryEntry entry in m_activePacketThreads)
			{
				try
				{
					var thread = (Thread) entry.Key;
					var client = (GameClient) entry.Value;

					builder.Append("Stack for thread from account: ");
					if (client != null && client.Account != null)
					{
						builder.Append(client.Account.Name);
						if (client.Player != null)
						{
							builder.Append(" (");
							builder.Append(client.Player.Name);
							builder.Append(")");
						}
					}
					else
					{
						builder.Append("null");
					}
					builder.Append("\n");
					builder.Append(Util.GetFormattedStackTraceFrom(Thread.CurrentThread));
					builder.Append("\n\n");
				}
				catch (Exception e)
				{
					builder.Append("Error getting stack for thread: ");
					builder.Append("\n");
					builder.Append(e);
					builder.Append("\n\n");
				}
			}
		}
		return builder.ToString();
#else
		return "LOGACTIVESTACKS is not defined in PacketProcessor";
#endif
	}


	public void HandlePacket(GSPacketIn packet)
	{
		if (packet == null || m_client == null)
			return;

		int code = packet.ID;

		Statistics.BytesIn += packet.PacketSize;
		Statistics.PacketsIn++;

		SavePacket(packet);

		IPacketHandler packetHandler = null;
		if (code < m_packetHandlers.Length)
		{
			packetHandler = m_packetHandlers[code];
		}

		else if (log.IsErrorEnabled)
		{
			log.ErrorFormat("Received packet code is outside of m_packetHandlers array bounds! " + m_client);
			log.Error(Marshal.ToHexDump(
				        String.Format("===> <{2}> Packet 0x{0:X2} (0x{1:X2}) length: {3} (ThreadId={4})", code, code ^ 168,
				                      (m_client.Account != null) ? m_client.Account.Name : m_client.TcpEndpoint,
				                      packet.PacketSize, Thread.CurrentThread.ManagedThreadId),
				        packet.ToArray()));
		}

		// make sure we can handle this packet at this stage
		var preprocess = m_packetPreprocessor.CanProcessPacket(m_client, packet);
		if(!preprocess)
		{
            // this packet can't be processed by this client right now, for whatever reason
            log.Info("PacketPreprocessor: Preprocessor prevents handling of a packet with packet.ID=" + packet.ID);
			return;
		}

		if (packetHandler != null)
		{
			Timer monitorTimer = null;
			if (log.IsDebugEnabled)
			{
				try
				{
					monitorTimer = new Timer(10000);
					m_activePacketHandler = packetHandler;
					m_handlerThreadID = Thread.CurrentThread.ManagedThreadId;
					monitorTimer.Elapsed += HandlePacketTimeout;
					monitorTimer.Start();
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error("Starting packet monitor timer", e);

					if (monitorTimer != null)
					{
						monitorTimer.Stop();
						monitorTimer.Close();
						monitorTimer = null;
					}
				}
			}

#if LOGACTIVESTACKS
			//Put the current thread into the active thread list!
			//No need to lock the hashtable since we created it
			//synchronized! One reader, multiple writers supported!
			m_activePacketThreads.Add(Thread.CurrentThread, m_client);
#endif
			long start = Environment.TickCount;
			try
			{
				packetHandler.HandlePacket(m_client, packet);
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
				{
					string client = (m_client == null ? "null" : m_client.ToString());
					log.Error(
						"Error while processing packet (handler=" + packetHandler.GetType().FullName + "  client: " + client + ")", e);
				}
			}
#if LOGACTIVESTACKS
			finally
			{
				//Remove the thread from the active list after execution
				//No need to lock the hashtable since we created it
				//synchronized! One reader, multiple writers supported!
				m_activePacketThreads.Remove(Thread.CurrentThread);
			}
#endif
			long timeUsed = Environment.TickCount - start;
			if (monitorTimer != null)
			{
				monitorTimer.Stop();
				monitorTimer.Close();
			}
			m_activePacketHandler = null;
			if (timeUsed > 1000)
			{
				string source = ((m_client.Account != null) ? m_client.Account.Name : m_client.TcpEndpoint);
				if (log.IsWarnEnabled)
					log.Warn("(" + source + ") Handle packet Thread " + Thread.CurrentThread.ManagedThreadId + " " + packetHandler +
					         " took " + timeUsed + "ms!");
			}
		}
	}
}