using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Project.Common;
using Project.Config;
using Project.Database.Connection;

namespace Project.GS
{
    public class GameServerConfiguration : BaseServerConfiguration
    {
        #region Logging
        /// <summary>
        /// The logger name where to log the gm+ commandos
        /// </summary>
        [JsonProperty]
        protected string m_gmActionsLoggerName;

        /// <summary>
        /// The logger name where to log cheat attempts
        /// </summary>
        [JsonProperty]
        protected string m_cheatLoggerName;

        /// <summary>
        /// The file name of the invalid names file
        /// </summary>
        [JsonProperty] 
        protected string m_invalidNamesFile = "";
        #endregion
        
        // 필요한 내용을 채워넣으세요.
        public GameServerConfiguration() : base()
        {
            m_ServerName = "PLZ SET SERVERNAME";
            m_ServerNameShort = "SERVER";
            m_serverType = eGameServerType.GST_Normal;
            
            if (Assembly.GetEntryAssembly() != null)
                m_rootDirectory = new FileInfo(Assembly.GetEntryAssembly().Location).DirectoryName;
            else
                m_rootDirectory = new FileInfo(Assembly.GetAssembly(typeof(GameServer)).Location).DirectoryName;

            m_logConfigFile = Path.Combine(Path.Combine(".", "config"), "logconfig.xml");

            m_scriptCompilationTarget = Path.Combine(Path.Combine(".", "lib"), "GameServerScripts.dll");
            
            
            m_dbType = ConnectionType.DATABASE_SQLITE;
            m_dbConnectionString = $"Data Source={Path.Combine(m_rootDirectory, "dol.sqlite3.db")}";
            
            m_autoSave = true;
            m_saveInterval = 10;
            m_maxClientCount = 500;
            
            m_cheatLoggerName = "cheats";
            m_gmActionsLoggerName = "gmactions";
            InventoryLoggerName = "inventories";
            m_invalidNamesFile = Path.Combine(Path.Combine(".", "config"), "invalidnames.txt");            
        }

        //-----------------------------------------------------------------------------------------------------------
        #region Properties
        /// <summary>
        /// Gets or sets the GM action logger name
        /// </summary>
        public string GMActionsLoggerName
        {
            get { return m_gmActionsLoggerName; }
            set { m_gmActionsLoggerName = value; }
        }

        /// <summary>
        /// Gets or sets the cheat logger name
        /// </summary>
        public string CheatLoggerName
        {
            get { return m_cheatLoggerName; }
            set { m_cheatLoggerName = value; }
        }
        public string DBConnectionString
        {
            get { return m_dbConnectionString; }
            set { m_dbConnectionString = value; }
        }

        /// <summary>
        /// Gets or sets the DB type
        /// </summary>
        public ConnectionType DBType
        {
            get { return m_dbType; }
            set { m_dbType = value; }
        }

        /// <summary>
        /// Gets or sets the autosave flag
        /// </summary>
        public bool AutoSave
        {
            get { return m_autoSave; }
            set { m_autoSave = value; }
        }        
        public int SaveInterval
        {
            get { return m_saveInterval; }
            set { m_saveInterval = value; }
        }
        public int CPUUse
        {
            get { return m_cpuUse; }
            set { m_cpuUse = value; }
        }		
        
        /// <summary>
        /// Gets or sets the max cout of clients allowed
        /// </summary>
        public int MaxClientCount
        {
            get { return m_maxClientCount; }
            set { m_maxClientCount = value; }
        }
        
        /// <summary>
        /// Gets or sets the trade logger name
        /// </summary>
        public string InventoryLoggerName { get; set; }
        #endregion
        
        #region Server

        /// <summary>
        /// holds the server root directory
        /// </summary>
        [JsonProperty]
        protected string m_rootDirectory;

        /// <summary>
        /// Holds the log configuration file path
        /// </summary>
        [JsonProperty]
        protected string m_logConfigFile;

        /// <summary>
        /// Name of the scripts compilation target
        /// </summary>
        [JsonProperty]
        protected string m_scriptCompilationTarget;

        /// <summary>
        /// The assemblies to include when compiling the scripts
        /// </summary>
        [JsonProperty]
        protected string m_scriptAssemblies;
		
        /// <summary>
        /// Enable/Disable Startup Script Compilation
        /// </summary>
        [JsonProperty]
        protected bool m_enableCompilation;

        /// <summary>
        /// True if the server shall automatically create accounts
        /// </summary>
        [JsonProperty]
        protected bool m_autoAccountCreation;

        /// <summary>
        /// The game server type
        /// </summary>
        [JsonProperty]
        protected eGameServerType m_serverType;

        /// <summary>
        /// The game server name
        /// </summary>
        [JsonProperty]
        protected string m_ServerName;

        /// <summary>
        /// The short server name, shown in /loc command
        /// </summary>
        [JsonProperty]
        protected string m_ServerNameShort;

        /// <summary>
        /// The count of server cpu
        /// </summary>
        [JsonProperty]
        protected int m_cpuCount = 1;
        [JsonProperty]
        private int m_cpuUse = 1;
       
        /// <summary>
        /// The max client count.
        /// </summary>
        [JsonProperty]
        protected int m_maxClientCount = 1000;
        #endregion
        
        #region Database

        /// <summary>
        /// The path to the XML database folder
        /// </summary>
        [JsonProperty]
        protected string m_dbConnectionString;

        /// <summary>
        /// Type database type
        /// </summary>
        [JsonProperty]
        protected ConnectionType m_dbType;

        /// <summary>
        /// True if the server shall autosave the db
        /// </summary>
        [JsonProperty]
        protected bool m_autoSave;

        /// <summary>
        /// The auto save interval in minutes
        /// </summary>
        [JsonProperty]
        protected int m_saveInterval;

        #endregion      
        
        /// <summary>
        /// Gets or sets the script assemblies to be included in the script compilation
        /// </summary>        
        public string[] AdditionalScriptAssemblies => string.IsNullOrEmpty(m_scriptAssemblies.Trim()) ? Array.Empty<string>() : m_scriptAssemblies.Split(',');
        
        /// <summary>
        /// Gets or sets the root directory of the server
        /// </summary>
        public string RootDirectory
        {
            get { return m_rootDirectory; }
            set { m_rootDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the log configuration file of this server
        /// </summary>
        public string LogConfigFile
        {
            get
            {
                if(Path.IsPathRooted(m_logConfigFile))
                    return m_logConfigFile;
                else
                    return Path.Combine(m_rootDirectory, m_logConfigFile);
            }
            set { m_logConfigFile = value; }
        }
        
        /// <summary>
        ///  config파일에 정의된 데이터로 서버 설정
        /// </summary>
        public override void OnLoadComplete()
        {
            if (m_cpuCount < 1)
                m_cpuCount = 1;	
            if (m_cpuUse < 1)
                m_cpuUse = 1;             
        }        
    }
}