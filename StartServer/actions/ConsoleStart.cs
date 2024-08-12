using System.Collections;
using System.Reflection;
using log4net;
using Project.Common;
using Project.Config;
using Project.GameServer;

namespace Server.Startup
{
    public class ConsoleStart : IAction
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        /// <summary>
        /// returns the name of this action
        /// </summary>
        public string Name
        {
            get { return "--start"; }
        }

        /// <summary>
        /// returns the syntax of this action
        /// </summary>
        public string Syntax
        {
            get { return "--start [-config=./config/serverconfig.xml] [-crashonfail]"; }
        }

        /// <summary>
        /// returns the description of this action
        /// </summary>
        public string Description
        {
            get { return " @ 게임서버를 콘솔모드로 시작합니다. 명령어를 입력이 가능합니다."; }
        }

        /// <summary>
        /// 크래쉬나면 중단
        /// </summary>
        private bool crashOnFail = false;
        
		private static bool StartServer()
		{
			Console.WriteLine("Starting the server");
			bool start = GameServer.Instance.Start();
			return start;
		}

		public void OnAction(Hashtable parameters)
		{
			Console.WriteLine("Starting GameServer ... please wait a moment!");
			FileInfo configFile;
			FileInfo currentAssembly = null;
			if (parameters["-config"] != null)
			{
				Console.WriteLine("Using config file: " + parameters["-config"]);
				configFile = new FileInfo((String)parameters["-config"]);
			}
			else
			{
				currentAssembly = new FileInfo(Assembly.GetEntryAssembly().Location);
				configFile = new FileInfo(currentAssembly.DirectoryName + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "serverconfig.xml");
			}
			if (parameters.ContainsKey("-crashonfail")) crashOnFail = true;
			var config = BaseServerConfiguration.GetConfigData<GameServerConfiguration>(configFile);
			if (config == null)
			{
				if (!configFile.Directory.Exists)
					configFile.Directory.Create();
				config.SaveJSON(configFile);
				
				Console.WriteLine("[경고] Game Server 설정 파일이 존재하지 않아서 기본 값으로 설정하고 저장함.");
			}
			

			GameServer.CreateInstance(config);
			StartServer();

			if (crashOnFail && GameServer.Instance.ServerStatus == eGameServerStatus.GSS_Closed)
			{
				throw new ApplicationException("Server did not start properly.");
			}

			bool run = true;
			while (run)
			{
				Console.Write("> ");
				string line = Console.ReadLine();

				switch (line.ToLower())
				{
					case "exit":
						run = false;
						break;
					// case "stacktrace":
					// 	log.Debug(PacketProcessor.GetConnectionThreadpoolStacks());
					// 	break;
					case "clear":
						Console.Clear();
						break;
					default:
						// if (line.Length <= 0)
						// 	break;
						// if (line[0] == '/')
						// {
						// 	line = line.Remove(0, 1);
						// 	line = line.Insert(0, "&");
						// }
						// GameClient client = new GameClient(null);
						// client.Out = new ConsolePacketLib();
						// try
						// {
						// 	bool res = ScriptMgr.HandleCommandNoPlvl(client, line);
						// 	if (!res)
						// 	{
						// 		Console.WriteLine("Unknown command: " + line);
						// 	}
						// }
						// catch (Exception e)
						// {
						// 	Console.WriteLine(e.ToString());
						// }
						break;
				}
			}
			if (GameServer.Instance != null)
				GameServer.Instance.Stop();
		}        
    }
}