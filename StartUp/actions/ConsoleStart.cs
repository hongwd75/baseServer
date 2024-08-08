using System.Reflection;
using log4net;

namespace Server.Startup
{
    public class ConsoleStart : IAction
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }
}