using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Core;
using Project.Common;
using Project.Config;
using Project.Database;
using Project.DataBase;
using Project.Database.Attributes;
using Project.GS.Events;
using Project.GS.ServerProperties;
using Project.Network;
using Project.Scheduler;

namespace Project.GS
{
    public class GameServer : BaseServer
    {
	    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Variables

        /// <summary>
        /// Minute conversion from milliseconds
        /// </summary>
        protected const int MINUTE_CONV = 60000;

        /// <summary>
        /// The instance!
        /// </summary>
        protected static GameServer m_instance;

        /// <summary>
        /// The textwrite for log operations
        /// </summary>
        protected ILog m_cheatLog;

        /// <summary>
        /// Database instance
        /// </summary>
        protected IObjectDatabase m_database;

        /// <summary>
        /// The textwrite for log operations
        /// </summary>
        protected ILog m_gmLog;

        /// <summary>
        /// Holds the startSystemTick when server is up.
        /// </summary>
        protected int m_startTick;

        /// <summary>
        /// Game server status variable
        /// </summary>
        protected eGameServerStatus m_status;

        /// <summary>
        /// World save timer
        /// </summary>
        protected Timer m_timer;

        /// <summary>
        /// A general logger for the server
        /// </summary>
        public ILog Logger
        {
            get { return log; }
        }
        #endregion
        
		#region Properties
		public static GameServer Instance => m_instance;
		public static IObjectDatabase Database => m_instance.DataBaseImpl;

		public new virtual GameServerConfiguration Configuration => (GameServerConfiguration) _config;
		public IObjectDatabase IDatabase => m_database;
		public eGameServerStatus ServerStatus => m_status;
		public SimpleScheduler Scheduler { get; protected set; }
		public PlayerManager PlayerManager { get; protected set; }
		public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

		protected virtual IObjectDatabase DataBaseImpl => Instance.m_database;

		/// <summary>
		/// Gets or sets the world save interval
		/// </summary>
		public int SaveInterval
		{
			get { return Configuration.SaveInterval; }
			set
			{
				Configuration.SaveInterval = value;
				if (m_timer != null)
					m_timer.Change(value*MINUTE_CONV, Timeout.Infinite);
			}
		}

		/// <summary>
		/// True if the server is listening
		/// </summary>
		public bool IsRunning
		{
			get { return _listen != null; }
		}

		/// <summary>
		/// Gets the number of millisecounds elapsed since the GameServer started.
		/// </summary>
		public int TickCount
		{
			get { return Environment.TickCount - m_startTick; }
		}

		#endregion
		
        #region Initialization
        public static void CreateInstance(GameServerConfiguration config)
        {
            //Only one intance
            if (Instance != null)
                return;

            //Try to find the log.config file, if it doesn't exist
            //we create it
            var logConfig = new FileInfo(config.LogConfigFile);
            if (!logConfig.Exists)
            {
                ResourceUtil.ExtractResource("logconfig.xml", logConfig.FullName);
            }

            //Configure and watch the config file
            XmlConfigurator.ConfigureAndWatch(logConfig);

            //Create the instance
            m_instance = new GameServer(config);
        }
        #endregion

        #region Constructors
        protected GameServer()
	        : this(new GameServerConfiguration()) { }

        protected GameServer(GameServerConfiguration config)
	        : base(config)
        {
	        m_gmLog = LogManager.GetLogger(Configuration.GMActionsLoggerName);
	        m_cheatLog = LogManager.GetLogger(Configuration.CheatLoggerName);

	        if (log.IsDebugEnabled)
	        {
		        log.Debug("Current directory is: " + Directory.GetCurrentDirectory());
		        log.Debug("Gameserver root directory is: " + Configuration.RootDirectory);
		        log.Debug("Changing directory to root directory");
	        }
	        Directory.SetCurrentDirectory(Configuration.RootDirectory);

	        try
	        {
		        
		        CheckAndInitDB();

		        if (log.IsInfoEnabled)
			        log.Info("Game Server Initialization finished!");
	        }
	        catch (Exception e)
	        {
		        if (log.IsFatalEnabled)
			        log.Fatal("GameServer initialization failed!", e);
		        throw new ApplicationException("Fatal Error: Could not initialize Game Server", e);
	        }
        }

        protected virtual void CheckAndInitDB()
        {
	        if (!InitDB() || m_database == null)
	        {
		        if (log.IsErrorEnabled)
			        log.Error("Could not initialize DB, please check path/connection string");
		        throw new ApplicationException("DB initialization error");
	        }
        }
        #endregion
        
		#region Database
		public bool InitDB()
		{
			if (m_database == null)
			{
				m_database = ObjectDatabase.GetObjectDatabase(Configuration.DBType, Configuration.DBConnectionString);

				try
				{
					//We will search our assemblies for DataTables by reflection so
					//it is not neccessary anymore to register new tables with the
					//server, it is done automatically!
					foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
                        // Walk through each type in the assembly
					    assembly.GetTypes().AsParallel().ForAll(type =>
					    {
					        if (!type.IsClass || type.IsAbstract)
					        {
					            return;
					        }

					        var attrib = type.GetCustomAttributes<DataTable>(false);
					        if (attrib.Any())
					        {
					            if (log.IsInfoEnabled)
					            {
					                log.InfoFormat("Registering table: {0}", type.FullName);
					            }

					            m_database.RegisterDataObject(type);
					        }
                        });
					}
				}
				catch (DatabaseException e)
				{
					if (log.IsErrorEnabled)
						log.Error("Error registering Tables", e);
					return false;
				}
			}
			if (log.IsInfoEnabled)
				log.Info("Database Initialization: true");
			return true;
		}

		/// <summary>
		/// Function called at X interval to write the database to disk
		/// </summary>
		/// <param name="sender">Object that generated the event</param>
		protected void SaveTimerProc(object sender)
		{
			try
			{
				int startTick = Environment.TickCount;
				if (log.IsInfoEnabled)
					log.Info("Saving database...");
				if (log.IsDebugEnabled)
					log.Debug("Save ThreadId=" + Thread.CurrentThread.ManagedThreadId);
				int saveCount = 0;
				if (m_database != null)
				{
					ThreadPriority oldprio = Thread.CurrentThread.Priority;
					Thread.CurrentThread.Priority = ThreadPriority.Lowest;

					// 다른 DB 다 저장하는 로직 추가해야함.
					
					// 2008-01-29 Kakuri - Obsolete
					//m_database.WriteDatabaseTables();
					Thread.CurrentThread.Priority = oldprio;
				}
				if (log.IsInfoEnabled)
					log.Info("Saving database complete!");
				startTick = Environment.TickCount - startTick;
				if (log.IsInfoEnabled)
					log.Info("Saved all databases and " + saveCount + " players in " + startTick + "ms");
			}
			catch (Exception e1)
			{
				if (log.IsErrorEnabled)
					log.Error("SaveTimerProc", e1);
			}
			finally
			{
				if (m_timer != null)
					m_timer.Change(SaveInterval*MINUTE_CONV, Timeout.Infinite);
				GameEventMgr.Notify(GameServerEvent.WorldSave);
			}
		}

		#endregion        
        
		#region Start
		public override bool Start()
		{
			try
			{
				if (log.IsDebugEnabled)
					log.DebugFormat("Starting Server, Memory is {0}MB", GC.GetTotalMemory(false)/1024/1024);
				
				m_status = eGameServerStatus.GSS_Closed;
				Thread.CurrentThread.Priority = ThreadPriority.Normal;

				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
				
				//---------------------------------------------------------------
				//Try to compile the Scripts
				if (!InitComponent(CompileScripts(), "Script compilation"))
					return false;
				
				//---------------------------------------------------------------
				//Try to init Server Properties
				if (!InitComponent(Properties.InitProperties, "Server Properties Lookup"))
					return false;
				
				//---------------------------------------------------------------
				//Check and update the database if needed
				if (!UpdateDatabase())
					return false;

				//---------------------------------------------------------------
				//Try to init the server port
				if (!InitComponent(InitSocket(), "InitSocket()"))
					return false;

				//---------------------------------------------------------------
				//Packet buffers
				if (!InitComponent(AllocatePacketBuffers(), "AllocatePacketBuffers()"))
					return false;
				

				//---------------------------------------------------------------
				//Try to init the RSA key
				/* No Cryptlib currently
					if (log.IsInfoEnabled)
						log.Info("Generating RSA key, may take a minute, please wait...");
					if (!InitComponent(CryptLib168.GenerateRSAKey(), "RSA key generation"))
						return false;
				 */

				//---------------------------------------------------------------
				//Try to initialize the Scheduler
				if (!InitComponent(() => Scheduler = new SimpleScheduler(), "Scheduler Initialization"))
					return false;

				
				//---------------------------------------------------------------
				//Try to initialize the PlayerManager
				if (!InitComponent(() => PlayerManager = new PlayerManager(this), "Player Manager Initialization"))
					return false;
				
				//---------------------------------------------------------------
				//Enable Worldsave timer now
				if (m_timer != null)
				{
					m_timer.Change(Timeout.Infinite, Timeout.Infinite);
					m_timer.Dispose();
				}
				m_timer = new Timer(SaveTimerProc, null, SaveInterval*MINUTE_CONV, Timeout.Infinite);
				if (log.IsInfoEnabled)
					log.Info("World save timer: true");
				

				//---------------------------------------------------------------
				//Notify our scripts that everything went fine!
				GameEventMgr.Notify(ScriptEvent.Loaded);

				//---------------------------------------------------------------
				//Set the GameServer StartTick
				m_startTick = Environment.TickCount;

				//---------------------------------------------------------------
				//Notify everyone that the server is now started!
				GameEventMgr.Notify(GameServerEvent.Started, this);

				//---------------------------------------------------------------
				//Try to start the base server (open server port for connections)
				if (!InitComponent(base.Start(), "base.Start()"))
					return false;

				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

				//---------------------------------------------------------------
				//Open the server, players can now connect if webhook, inform Discord!
				m_status = eGameServerStatus.GSS_Open;
				
				if (log.IsInfoEnabled)
					log.Info($"GameServer {Version} is now open for connections!");

				//INIT WAS FINE!
				return true;
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("Failed to start the server", e);

				return false;
			}
		}

		/// <summary>
		/// Logs unhandled exceptions
		/// </summary>
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			log.Fatal("Unhandled exception!\n" + e.ExceptionObject);
			if (e.IsTerminating)
				LogManager.Shutdown();
		}

		public bool CompileScripts()
		{
			string scriptDirectory = Path.Combine(Configuration.RootDirectory, "scripts");
			if (!Directory.Exists(scriptDirectory))
				Directory.CreateDirectory(scriptDirectory);
			
			bool compiled = false;
			
			// Check if Configuration Forces to use Pre-Compiled Game Server Scripts Assembly
			if (!Configuration.EnableCompilation)
			{
				log.Info("Script Compilation Disabled in Server Configuration, Loading pre-compiled Assembly...");

				if (File.Exists(Configuration.ScriptCompilationTarget))
				{
					ScriptMgr.LoadAssembly(Configuration.ScriptCompilationTarget);
				}
				else
				{
					log.WarnFormat("Compilation Disabled - Could not find pre-compiled Assembly : {0} - Server starting without Scripts Assembly!", Configuration.ScriptCompilationTarget);
				}
				
				compiled = true;
			}
			else
			{
				compiled = ScriptMgr.CompileScripts(false, scriptDirectory, Configuration.ScriptCompilationTarget, Configuration.AdditionalScriptAssemblies);
			}
			
			if (compiled)
			{
				//---------------------------------------------------------------
				//Register Script Tables
				if (log.IsInfoEnabled)
					log.Info("GameServerScripts Tables Initializing...");
				
				try
				{
					// Walk through each assembly in scripts
					foreach (Assembly asm in ScriptMgr.Scripts)
					{
						// Walk through each type in the assembly
						foreach (Type type in asm.GetTypes())
						{
							if (type.IsClass != true || !typeof(DataObject).IsAssignableFrom(type))
								continue;
							
							object[] attrib = type.GetCustomAttributes(typeof(DataTable), false);
							if (attrib.Length > 0)
							{
								if (log.IsInfoEnabled)
									log.Info("Registering Scripts table: " + type.FullName);
								
								Database.RegisterDataObject(type);
							}
						}
					}
				}
				catch (DatabaseException dbex)
				{
					if (log.IsErrorEnabled)
						log.Error("Error while registering Script Tables", dbex);
					
					return false;
				}
				
	        	if (log.IsInfoEnabled)
					log.Info("GameServerScripts Database Tables Initialization: true");
	        	
	        	return true;
			}
			
			return false;
		}

		
		protected virtual bool UpdateDatabase()
		{
			bool result = true;
			try
			{
				log.Info("Checking database for updates ...");
				
				foreach (Assembly asm in ScriptMgr.GameServerScripts)
				{

					foreach (Type type in asm.GetTypes())
					{
						if (!type.IsClass)
							continue;
						if (!typeof(IDatabaseUpdater).IsAssignableFrom(type))
							continue;
						
						object[] attributes = type.GetCustomAttributes(typeof (DatabaseUpdateAttribute), false);
						if (attributes.Length <= 0)
							continue;
	
						try
						{
							var instance = Activator.CreateInstance(type) as IDatabaseUpdater;
							instance.Update();
						}
						catch (Exception uex)
						{
							if (log.IsErrorEnabled)
								log.ErrorFormat("Error While Updating Database with Script {0} - {1}", type, uex);
							
							result = false;
						}
					}
				}
			}
			catch (Exception e)
			{
				log.Error("Error checking/updating database: ", e);
				return false;
			}

			log.Info("Database update complete.");
			return result;
		}

		/// <summary>
		/// Prints out some text info on component initialisation
		/// and stops the server again if the component failed
		/// </summary>
		/// <param name="componentInitState">The state</param>
		/// <param name="text">The text to print</param>
		/// <returns>false if startup should be interrupted</returns>
		protected bool InitComponent(bool componentInitState, string text)
		{
			if (log.IsDebugEnabled)
				log.DebugFormat("Start Memory {0}: {1}MB", text, GC.GetTotalMemory(false)/1024/1024);
			
			if (log.IsInfoEnabled)
				log.InfoFormat("{0}: {1}", text, componentInitState);
			
			if (!componentInitState)
				Stop();
			
			if (log.IsDebugEnabled)
				log.DebugFormat("Finish Memory {0}: {1}MB", text, GC.GetTotalMemory(false)/1024/1024);
			
			return componentInitState;
		}

		protected bool InitComponent(Action componentInitMethod, string text)
		{
			if (log.IsDebugEnabled)
				log.DebugFormat("Start Memory {0}: {1}MB", text, GC.GetTotalMemory(false)/1024/1024);
			
			bool componentInitState = false;
			try
			{
				componentInitMethod();
				componentInitState = true;
			}
			catch (Exception ex)
			{
				if (log.IsErrorEnabled)
					log.ErrorFormat("{0}: Error While Initialization\n{1}", text, ex);
			}

			if (log.IsInfoEnabled)
				log.InfoFormat("{0}: {1}", text, componentInitState);

			if (!componentInitState)
				Stop();
			
			if (log.IsDebugEnabled)
				log.DebugFormat("Finish Memory {0}: {1}MB", text, GC.GetTotalMemory(false)/1024/1024);
			
			return componentInitState;
		}
		#endregion

		
		
		#region Stop
		public void Close()
		{
			m_status = eGameServerStatus.GSS_Closed;
		}

		public void Open()
		{
			m_status = eGameServerStatus.GSS_Open;
		}

		/// <summary>
		/// Stops the server, disconnects all clients, and writes the database to disk
		/// </summary>
		public override void Stop()
		{
			//Stop new clients from logging in
			m_status = eGameServerStatus.GSS_Closed;

			log.Info("GameServer.Stop() - enter method");

			if (log.IsWarnEnabled)
			{
				string stacks = PacketProcessor.GetConnectionThreadpoolStacks();
				if (stacks.Length > 0)
				{
					log.Warn("Packet processor thread stacks:");
					log.Warn(stacks);
				}
			}

			//Notify our scripthandlers
			GameEventMgr.Notify(ScriptEvent.Unloaded);

			//Notify of the global server stop event
			//We notify before we shutdown the database
			//so that event handlers can use the datbase too
			GameEventMgr.Notify(GameServerEvent.Stopped, this);
			GameEventMgr.RemoveAllHandlers(true);

			//Stop the World Save timer
			if (m_timer != null)
			{
				m_timer.Change(Timeout.Infinite, Timeout.Infinite);
				m_timer.Dispose();
				m_timer = null;
			}

			//Stop the base server
			base.Stop();
			

			//Stop the WorldMgr, save all players
			//WorldMgr.SaveToDatabase();
			SaveTimerProc(null);
			

			// Stop Server Scheduler
			if (Scheduler != null)
				Scheduler.Shutdown();
			Scheduler = null;
			
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

			if (log.IsInfoEnabled)
				log.Info("Server Stopped");

			LogManager.Shutdown();
		}

		#endregion

		#region Packet buffer pool

		/// <summary>
		/// The size of all packet buffers.
		/// </summary>
		private const int BUF_SIZE = 2048;

		/// <summary>
		/// Holds all packet buffers.
		/// </summary>
		private Queue<byte[]> m_packetBufPool;
		private object m_packetBufPoolLock = new object();

		public int MaxPacketPoolSize
		{
			get { return Configuration.MaxClientCount*3; }
		}
		
		/// <summary>
		/// Gets the count of packet buffers in the pool.
		/// </summary>
		public int PacketPoolSize
		{
			get
			{
				int packetBufCount = 0;
				
				lock(m_packetBufPoolLock)
					packetBufCount = m_packetBufPool.Count;
				
				return packetBufCount;
			}
		}

		/// <summary>
		/// Allocates all packet buffers.
		/// </summary>
		/// <returns>success</returns>
		private bool AllocatePacketBuffers()
		{
			int count = MaxPacketPoolSize;

			lock(m_packetBufPoolLock)
			{
				m_packetBufPool = new Queue<byte[]>(count);
			
				for (int i = 0; i < count; i++)
				{
					m_packetBufPool.Enqueue(new byte[BUF_SIZE]);
				}
			}
	
			if (log.IsDebugEnabled)
				log.DebugFormat("allocated packet buffers: {0}", count.ToString());

			return true;
		}

		/// <summary>
		/// Gets packet buffer from the pool.
		/// </summary>
		/// <returns>byte array that will be used as packet buffer.</returns>
		public override byte[] AcquirePacketBuffer()
		{
			lock (m_packetBufPoolLock)
			{
				if (m_packetBufPool.Count > 0)
					return m_packetBufPool.Dequeue();
			}

			log.Warn("packet buffer pool is empty!");

			return new byte[BUF_SIZE];
		}

		/// <summary>
		/// Releases previously acquired packet buffer.
		/// </summary>
		/// <param name="buf">The released buf</param>
		public override void ReleasePacketBuffer(byte[] buf)
		{
			if (buf == null)
				return;

			lock (m_packetBufPoolLock)
			{
				if (m_packetBufPool.Count < MaxPacketPoolSize)
					m_packetBufPool.Enqueue(buf);
			}
		}

		#endregion

		#region Client

		/// <summary>
		/// Creates a new client
		/// </summary>
		/// <returns>An instance of a new client</returns>
		protected override BaseClient GetNewClient()
		{
			var client = new GameClient(this);
			GameEventMgr.Notify(GameClientEvent.Created, client);
			client.UdpConfirm = false;

			return client;
		}

		#endregion

		#region Logging

		/// <summary>
		/// Writes a line to the gm log file
		/// </summary>
		/// <param name="text">the text to log</param>
		public void LogGMAction(string text)
		{
			m_gmLog.Logger.Log(typeof (GameServer), Level.Alert, text, null);
		}

		/// <summary>
		/// Writes a line to the cheat log file
		/// </summary>
		/// <param name="text">the text to log</param>
		public void LogCheatAction(string text)
		{
			m_cheatLog.Logger.Log(typeof (GameServer), Level.Alert, text, null);
			log.Debug(text);
		}
		#endregion        
    }
}