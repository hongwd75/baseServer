using System.Net;
using System.Text;
using Newtonsoft.Json;


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
		[JsonProperty]
		private IPAddress _ip;

		/// <summary>
		/// The listening port of the server.
		/// </summary>
		[JsonProperty]
		private ushort _port;

		/// <summary>
		/// Constructs a server configuration with default values.
		/// </summary>
		protected BaseServerConfiguration()
		{
			_port = 10300;
			_ip = IPAddress.Any;
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
		///  config 파일 읽기
		/// </summary>
		/// <param name="filePath"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetConfigData<T>(FileInfo fileInfo) where T : class
		{
			if (fileInfo.Exists)
			{
				using (StreamReader reader = fileInfo.OpenText())
				{
					string jsonString = reader.ReadToEnd();
					return JsonConvert.DeserializeObject<T>(jsonString);
				}
			}
			else
			{
				Console.WriteLine("파일이 존재하지 않습니다.");
				return null;
			}
		}

		/// <summary>
		///  config 저장
		/// </summary>
		/// <param name="filePath"></param>
		public void SaveJSON(FileInfo configFile)
		{
			if (configFile.Exists)
				configFile.Delete();
			
			string jsonString = JsonConvert.SerializeObject(this);
			File.WriteAllText(configFile.FullName, jsonString, Encoding.UTF8);
			Console.WriteLine("JSON 저장 완료: " + jsonString);			
		}

		/// <summary>
		///  config 파일 로드가 완료되고 설정하는 내용들 여기서 정의 
		/// </summary>
		public virtual void OnLoadComplete()
		{
			
		}
	}    
}
