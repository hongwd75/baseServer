using System.Net;
using System.Net.Sockets;
using System.Reflection;
using log4net;
using Project.Common;
using Project.Config;

namespace Project.Network
{
    /// <summary>
    /// Base class for a server using overlapped socket IO.
    /// </summary>
    public class BaseServer
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// Holds the async accept callback delegate
        /// </summary>
        private readonly AsyncCallback _asyncAcceptCallback;

        /// <summary>
        /// Hash table of clients
        /// </summary>
        protected readonly HashSet<BaseClient> _clients = new HashSet<BaseClient>();
        protected readonly object _clientsLock = new object();

        /// <summary>
        /// The configuration of this server
        /// </summary>
        protected BaseServerConfiguration _config;

        /// <summary>
        /// Socket that receives connections
        /// </summary>
        protected Socket _listen;
        
        /// <summary>
        /// Constructor that takes a server configuration as parameter
        /// </summary>
        /// <param name="config">The configuraion for the server</param>
        protected BaseServer(BaseServerConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _config = config;
            _asyncAcceptCallback = new AsyncCallback(AcceptCallback);
        }

        /// <summary>
        /// Retrieves the server configuration
        /// </summary>
        public virtual BaseServerConfiguration Configuration
        {
            get { return _config; }
        }

        /// <summary>
		/// Returns the number of clients currently connected to the server
		/// </summary>
		public int ClientCount
		{
			get
			{
				int clientCount = 0;
				
				lock (_clientsLock)
				{
					clientCount = _clients.Count;
				}
				
				return clientCount;
			}
		}

		/// <summary>
		/// Creates a new client object
		/// </summary>
		/// <returns>A new client object</returns>
		protected virtual BaseClient GetNewClient()
		{
			return new BaseClient(this);
		}

		/// <summary>
		/// Used to get packet buffer.
		/// </summary>
		/// <returns>byte array that will be used as packet buffer.</returns>
		public virtual byte[] AcquirePacketBuffer()
		{
			return new byte[2048];
		}

		/// <summary>
		/// Releases previously acquired packet buffer.
		/// </summary>
		/// <param name="buf"></param>
		public virtual void ReleasePacketBuffer(byte[] buf)
		{
		}

		/// <summary>
		/// Initializes and binds the socket, doesn't listen yet!
		/// </summary>
		/// <returns>true if bound</returns>
		protected virtual bool InitSocket()
		{
			try
			{
				_listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_listen.Bind(new IPEndPoint(_config.IP, _config.Port));
			}
			catch (Exception e)
			{
				if (Log.IsErrorEnabled)
					Log.Error("InitSocket", e);

				return false;
			}

			return true;
		}

		/// <summary>
		/// Starts the server
		/// </summary>
		/// <returns>True if the server was successfully started</returns>
		public virtual bool Start()
		{
			//Test if we have a valid port yet
			//if not try  binding.
			if (_listen == null && !InitSocket())
				return false;

			try
			{
				_listen.Listen(100);
				_listen.BeginAccept(_asyncAcceptCallback, this);

				if (Log.IsDebugEnabled)
					Log.Debug("Server is now listening to incoming connections!");
			}
			catch (Exception e)
			{
				if (Log.IsErrorEnabled)
					Log.Error("Start", e);

				if (_listen != null)
					_listen.Close();

				return false;
			}

			return true;
		}

		/// <summary>
		/// Called when a client is trying to connect to the server
		/// </summary>
		/// <param name="ar">Async result of the operation</param>
		private void AcceptCallback(IAsyncResult ar)
		{
			Socket sock = null;

			try
			{
				if (_listen == null)
					return;

				sock = _listen.EndAccept(ar);

				sock.SendBufferSize = Constants.SendBufferSize;
				sock.ReceiveBufferSize = Constants.ReceiveBufferSize;
				sock.NoDelay = Constants.UseNoDelay;

				BaseClient baseClient = null;

				try
				{
                    // Removing this message in favor of connection message in GameClient
                    // This will also reduce spam when server is pinged with 0 bytes - Tolakram
					//string ip = sock.Connected ? sock.RemoteEndPoint.ToString() : "socket disconnected";
					//Log.Info("Incoming connection from " + ip);

					baseClient = GetNewClient();
					baseClient.Socket = sock;

					lock (_clientsLock)
						_clients.Add(baseClient);

					baseClient.OnConnect();
					baseClient.BeginReceive();
				}
				catch (SocketException)
				{
					Log.Error("BaseServer SocketException");
					if (baseClient != null)
						Disconnect(baseClient);
				}
				catch (Exception e)
				{
					Log.Error("Client creation", e);

					if (baseClient != null)
						Disconnect(baseClient);
				}
			}
			catch
			{
				Log.Error("AcceptCallback: Catch");

				if (sock != null) // don't leave the socket open on exception
				{
					try
					{
						sock.Close();
					}
					catch
					{
					}
				}
			}
			finally
			{
				if (_listen != null)
				{
					_listen.BeginAccept(_asyncAcceptCallback, this);
				}
			}
		}

		/// <summary>
		/// Stops the server
		/// </summary>
		public virtual void Stop()
		{
			if (Log.IsDebugEnabled)
				Log.Debug("Stopping server! - Entering method");

			try
			{
				if (_listen != null)
				{
					Socket socket = _listen;
					_listen = null;
					socket.Close();

					if (Log.IsDebugEnabled)
						Log.Debug("Server is no longer listening for incoming connections!");
				}
			}
			catch (Exception e)
			{
				if (Log.IsErrorEnabled)
					Log.Error("Stop", e);
			}

			lock (_clientsLock)
			{
				try
				{
					foreach (var client in _clients)
					{
						client.CloseConnections();
					}

					if (Log.IsDebugEnabled)
						Log.Debug("Stopping server! - Cleaning up client list!");

					_clients.Clear();
				}
				catch (Exception e)
				{
					if (Log.IsErrorEnabled)
						Log.Error("Stop", e);
				}
			}
			
			if (Log.IsDebugEnabled)
				Log.Debug("Stopping server! - End of method!");
		}

		/// <summary>
		/// Disconnects a client
		/// </summary>
		/// <param name="baseClient">Client to be disconnected</param>
		/// <returns>True if the client was disconnected, false if it doesn't exist</returns>
		public virtual bool Disconnect(BaseClient baseClient)
		{
			lock (_clientsLock)
			{
				if (!_clients.Contains(baseClient))
					return false;

				_clients.Remove(baseClient);
			}

			try
			{
				baseClient.OnDisconnect();
				baseClient.CloseConnections();
			}
			catch (Exception e)
			{
				if (Log.IsErrorEnabled)
					Log.Error("Exception", e);

				return false;
			}

			return true;
		}        
    }
}