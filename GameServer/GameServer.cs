using System.Net.Sockets;
using System.Reflection;
using log4net;
using log4net.Config;
using Project.Common;
using Project.Config;
using Project.Network;

namespace Project.GameServer
{
    public class GameServer : BaseServer
    {
        protected GameServer()
            : this(new GameServerConfiguration()) { }        
        protected GameServer(BaseServerConfiguration config) : base(config)
        {
        }
        
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
		public Scheduler.SimpleScheduler Scheduler { get; protected set; }
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
    }
}