using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PBase.Config;


namespace Project.Config
{
	/// <summary>
	/// Base configuration for the server.
	/// </summary>
	public class BaseServerConfiguration
	{
		/// <summary>
		/// The listening address of the server.
		/// </summary>
		private IPAddress _ip;

		private string ipInfo;
		/// <summary>
		/// The listening port of the server.
		/// </summary>
		private ushort _port;

		/// <summary>
		/// The region (external) address of the server.
		/// </summary>
		private IPAddress _regionIP;

		/// <summary>
		/// The region (external) port of the server.
		/// </summary>
		private ushort _regionPort;
		
		/// <summary>
		/// Constructs a server configuration with default values.
		/// </summary>
		protected BaseServerConfiguration()
		{
			_port = 10300;
			_ip = IPAddress.Any;
			_regionIP = IPAddress.Any;
			_regionPort = 10400;			
		}

		/// <summary>
		/// Gets/sets the listening port for the server.
		/// </summary>
		public ushort Port
		{
			get { return _port; }
			set { _port = value; }
		}

		/// <summary>
		/// Gets/sets the listening address for the server.
		/// </summary>
		public IPAddress IP
		{
			get { return _ip; }
			set { _ip = value; }
		}

		/// <summary>
		/// Loads the configuration values from the given configuration element.
		/// </summary>
		/// <param name="root">the root config element</param>
		protected virtual void LoadFromConfig(ConfigElement root)
		{
			string ip = root["Server"]["IP"].GetString("any");
			_ip = ip == "any" ? IPAddress.Any : IPAddress.Parse(ip);
			_port = (ushort) root["Server"]["Port"].GetInt(_port);

			ip = root["Server"]["RegionIP"].GetString("any");
			_regionIP = ip == "any" ? IPAddress.Any : IPAddress.Parse(ip);
			_regionPort = (ushort) root["Server"]["RegionPort"].GetInt(_regionPort);

			ip = root["Server"]["UdpIP"].GetString("any");
		}

		/// <summary>
		/// Load the configuration from an XML source file.
		/// </summary>
		/// <param name="configFile">the file to load from</param>
		public void LoadFromXMLFile(FileInfo configFile)
		{
			if (configFile == null)
				throw new ArgumentNullException("configFile");

			XMLConfigFile xmlConfig = XMLConfigFile.ParseXMLFile(configFile);
			LoadFromConfig(xmlConfig);
		}

		/// <summary>
		/// Saves the values to the given configuration element.
		/// </summary>
		/// <param name="root">the configuration element to save to</param>
		protected virtual void SaveToConfig(ConfigElement root)
		{
			root["Server"]["Port"].Set(_port);
			root["Server"]["IP"].Set(_ip);
			root["Server"]["RegionIP"].Set(_regionIP);
			root["Server"]["RegionPort"].Set(_regionPort);
		}

		/// <summary>
		/// Saves the values to the given XML configuration file.
		/// </summary>
		/// <param name="configFile">the file to save to</param>
		public void SaveToXMLFile(FileInfo configFile)
		{
			if (configFile == null)
				throw new ArgumentNullException("configFile");

			var config = new XMLConfigFile();
			SaveToConfig(config);

			config.Save(configFile);
		}
	}
}
