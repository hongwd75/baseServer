using System.Reflection;
using log4net;
using Project.DataBase;
using Project.Network;

namespace Project.GS;

public class GameClient : BaseClient, ICustomParamsValuable
{
    #region eClientState enum

    /// <summary>
    /// Current state of the client
    /// </summary>
    public enum eClientState
    {
        NotConnected = 0x00,
        Connecting = 0x01,
        CharScreen = 0x02,
        WorldEnter = 0x03,
        Playing = 0x04,
        Linkdead = 0x05,
        Disconnected = 0x06,
    } ;

    #endregion
    
    /// <summary>
    /// Custom Account Params
    /// </summary>
    protected Dictionary<string, List<string>> m_customParams = new Dictionary<string, List<string>>();
    
    /// <summary>
    /// Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// This variable holds the accountdata
    /// </summary>
    protected Account m_account;
    
    /// <summary>
    /// Holds the current clientstate
    /// </summary>
    protected volatile eClientState m_clientState = eClientState.NotConnected;
    
    /// <summary>
    /// This variable holds the sessionid
    /// </summary>
    protected int m_sessionID;
    
    /// <summary>
    /// Gets or sets the account being used by this client
    /// </summary>
    public Account Account
    {
        get { return m_account; }
        set
        {
            m_account = value;
            // Load Custom Params
            this.InitFromCollection<AccountXCustomParam>(value.CustomParams, param => param.KeyName, param => param.Value);
            GameEventMgr.Notify(GameClientEvent.AccountLoaded, this);
        }
    }
    
    /// <summary>
    /// Get the Custom Params from this Game Client
    /// </summary>
    public Dictionary<string, List<string>> CustomParamsDictionary
    {
        get { return m_customParams; }
        set
        {
            Account.CustomParams = value.SelectMany(kv => kv.Value.Select(val => new AccountXCustomParam(Account.Name, kv.Key, val))).ToArray();
            m_customParams = value;
        }
    }    
}