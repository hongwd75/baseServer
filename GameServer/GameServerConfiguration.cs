using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using PBase.Config;
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
        protected string m_gmActionsLoggerName;

        /// <summary>
        /// The logger name where to log cheat attempts
        /// </summary>
        protected string m_cheatLoggerName;

        /// <summary>
        /// The file name of the invalid names file
        /// </summary>
        protected string m_invalidNamesFile = "";
        #endregion
        
		#region Load/Save
		
		/// <summary>
		/// Loads the config values from a specific config element
		/// </summary>
		/// <param name="root">the root config element</param>
		protected override void LoadFromConfig(ConfigElement root)
		{
			base.LoadFromConfig(root);

			// Removed to not confuse users
//			m_rootDirectory = root["Server"]["RootDirectory"].GetString(m_rootDirectory);

			m_logConfigFile = root["Server"]["LogConfigFile"].GetString(m_logConfigFile);

			m_scriptCompilationTarget = root["Server"]["ScriptCompilationTarget"].GetString(m_scriptCompilationTarget);
			m_scriptAssemblies = root["Server"]["ScriptAssemblies"].GetString(m_scriptAssemblies);
			m_enableCompilation = root["Server"]["EnableCompilation"].GetBoolean(true);
			m_autoAccountCreation = root["Server"]["AutoAccountCreation"].GetBoolean(m_autoAccountCreation);

			string serverType = root["Server"]["GameType"].GetString("Normal");
			switch (serverType.ToLower())
			{
				case "normal":
					m_serverType = eGameServerType.GST_Normal;
					break;
				case "test":
					m_serverType = eGameServerType.GST_Test;
					break;
				default:
					m_serverType = eGameServerType.GST_Normal;
					break;
			}

			m_ServerName = root["Server"]["ServerName"].GetString(m_ServerName);
			m_ServerNameShort = root["Server"]["ServerNameShort"].GetString(m_ServerNameShort);

			m_cheatLoggerName = root["Server"]["CheatLoggerName"].GetString(m_cheatLoggerName);
			m_gmActionsLoggerName = root["Server"]["GMActionLoggerName"].GetString(m_gmActionsLoggerName);
			m_invalidNamesFile = root["Server"]["InvalidNamesFile"].GetString(m_invalidNamesFile);

			string db = root["Server"]["DBType"].GetString("XML");
			switch (db.ToLower())
			{
				case "xml":
					m_dbType = ConnectionType.DATABASE_XML;
					break;
				case "mysql":
					m_dbType = ConnectionType.DATABASE_MYSQL;
					break;
				case "sqlite":
					m_dbType = ConnectionType.DATABASE_SQLITE;
					break;
				case "mssql":
					m_dbType = ConnectionType.DATABASE_MSSQL;
					break;
				case "odbc":
					m_dbType = ConnectionType.DATABASE_ODBC;
					break;
				case "oledb":
					m_dbType = ConnectionType.DATABASE_OLEDB;
					break;
				default:
					m_dbType = ConnectionType.DATABASE_XML;
					break;
			}
			m_dbConnectionString = root["Server"]["DBConnectionString"].GetString(m_dbConnectionString);
			m_autoSave = root["Server"]["DBAutosave"].GetBoolean(m_autoSave);
			m_saveInterval = root["Server"]["DBAutosaveInterval"].GetInt(m_saveInterval);
			m_maxClientCount = root["Server"]["MaxClientCount"].GetInt(m_maxClientCount);
			m_cpuCount = root["Server"]["CpuCount"].GetInt(m_cpuCount);
			
			if (m_cpuCount < 1)
				m_cpuCount = 1;
			
			m_cpuUse = root["Server"]["CpuUse"].GetInt(m_cpuUse);
			if (m_cpuUse < 1)
				m_cpuUse = 1; 
		}

		/// <summary>
		/// Saves the values into a specific config element
		/// </summary>
		/// <param name="root">the root config element</param>
		protected override void SaveToConfig(ConfigElement root)
		{
			base.SaveToConfig(root);
			root["Server"]["ServerName"].Set(m_ServerName);
			root["Server"]["ServerNameShort"].Set(m_ServerNameShort);
			// Removed to not confuse users
//			root["Server"]["RootDirectory"].Set(m_rootDirectory);
			root["Server"]["LogConfigFile"].Set(m_logConfigFile);

			root["Server"]["ScriptCompilationTarget"].Set(m_scriptCompilationTarget);
			root["Server"]["ScriptAssemblies"].Set(m_scriptAssemblies);
			root["Server"]["EnableCompilation"].Set(m_enableCompilation);
			root["Server"]["AutoAccountCreation"].Set(m_autoAccountCreation);

			string serverType = "Normal";

			switch (m_serverType)
			{
				case eGameServerType.GST_Normal:
					serverType = "Normal";
					break;
				case eGameServerType.GST_Test:
					serverType = "Test";
					break;
				default:
					serverType = "Normal";
					break;
			}
			root["Server"]["GameType"].Set(serverType);

			root["Server"]["CheatLoggerName"].Set(m_cheatLoggerName);
			root["Server"]["GMActionLoggerName"].Set(m_gmActionsLoggerName);
			root["Server"]["InvalidNamesFile"].Set(m_invalidNamesFile);

			string db = "XML";
			
			switch (m_dbType)
			{
			case ConnectionType.DATABASE_XML:
				db = "XML";
					break;
			case ConnectionType.DATABASE_MYSQL:
				db = "MYSQL";
					break;
			case ConnectionType.DATABASE_SQLITE:
				db = "SQLITE";
					break;
			case ConnectionType.DATABASE_MSSQL:
				db = "MSSQL";
					break;
			case ConnectionType.DATABASE_ODBC:
				db = "ODBC";
					break;
			case ConnectionType.DATABASE_OLEDB:
				db = "OLEDB";
					break;
				default:
					m_dbType = ConnectionType.DATABASE_XML;
					break;
			}
			root["Server"]["DBType"].Set(db);
			root["Server"]["DBConnectionString"].Set(m_dbConnectionString);
			root["Server"]["DBAutosave"].Set(m_autoSave);
			root["Server"]["DBAutosaveInterval"].Set(m_saveInterval);
			root["Server"]["CpuUse"].Set(m_cpuUse);
		}
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
            m_enableCompilation = true;
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
        protected string m_rootDirectory;

        /// <summary>
        /// Holds the log configuration file path
        /// </summary>
        protected string m_logConfigFile;

        /// <summary>
        /// Name of the scripts compilation target
        /// </summary>
        protected string m_scriptCompilationTarget;

        /// <summary>
        /// The assemblies to include when compiling the scripts
        /// </summary>
        protected string m_scriptAssemblies;
		
        /// <summary>
        /// Enable/Disable Startup Script Compilation
        /// </summary>
        protected bool m_enableCompilation;

        /// <summary>
        /// True if the server shall automatically create accounts
        /// </summary>
        protected bool m_autoAccountCreation;

        /// <summary>
        /// The game server type
        /// </summary>
        protected eGameServerType m_serverType;

        /// <summary>
        /// The game server name
        /// </summary>
        protected string m_ServerName;

        /// <summary>
        /// The short server name, shown in /loc command
        /// </summary>
        protected string m_ServerNameShort;

        /// <summary>
        /// The count of server cpu
        /// </summary>
        protected int m_cpuCount = 1;
        private int m_cpuUse = 1;
       
        /// <summary>
        /// The max client count.
        /// </summary>
        protected int m_maxClientCount = 1000;
        #endregion
        
        #region Database

        /// <summary>
        /// The path to the XML database folder
        /// </summary>
        protected string m_dbConnectionString;

        /// <summary>
        /// Type database type
        /// </summary>
        protected ConnectionType m_dbType;

        /// <summary>
        /// True if the server shall autosave the db
        /// </summary>
        protected bool m_autoSave;

        /// <summary>
        /// The auto save interval in minutes
        /// </summary>
        protected int m_saveInterval;

        #endregion

        /// <summary>
        /// Gets or sets the script assemblies to be included in the script compilation
        /// </summary>
        public string[] AdditionalScriptAssemblies => string.IsNullOrEmpty(m_scriptAssemblies?.Trim()) ? Array.Empty<string>() : m_scriptAssemblies.Split(',');
        
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
        /// Get or Set the Compilation Flag
        /// </summary>
        public bool EnableCompilation
        {
            get { return m_enableCompilation; }
            set { m_enableCompilation = value; }
        }
        
        /// <summary>
        /// Gets or sets the script compilation target
        /// </summary>
        public string ScriptCompilationTarget
        {
            get { return m_scriptCompilationTarget; }
            set { m_scriptCompilationTarget = value; }
        }        

        /// <summary>
        /// Gets or sets the invalid name filename
        /// </summary>
        public string InvalidNamesFile
        {
            get
            {
                if(Path.IsPathRooted(m_invalidNamesFile))
                    return m_invalidNamesFile;
                else
                    return Path.Combine(m_rootDirectory, m_invalidNamesFile);
            }
            set { m_invalidNamesFile = value; }
        }        
    }
}