using System.Reflection;
using log4net;
using Project.DataBase;
using Project.GS.Events;
using Project.GS.PacketHandler;
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
    /// Gets or sets the client state
    /// </summary>
    public eClientState ClientState
    {
        get { return m_clientState; }
        set
        {
            eClientState oldState = m_clientState;

            // refresh ping timeouts immediately when we change into playing state or charscreen
            if ((oldState != eClientState.Playing && value == eClientState.Playing) ||
                (oldState != eClientState.CharScreen && value == eClientState.CharScreen))
            {
                PingTime = DateTime.Now.Ticks;
            }

            m_clientState = value;
            GameEventMgr.Notify(GameClientEvent.StateChanged, this);
            //DOLConsole.WriteSystem("New State="+value.ToString());
        }
    }
    
    /// <summary>
    /// Variable is false if account/player is Ban, for a wrong password, if server is closed etc ... 
    /// </summary>
    public bool IsConnected = true;

    /// <summary>
    /// Gets whether or not the client is playing
    /// </summary>
    public bool IsPlaying
    {
        get
        {
            //Linkdead players also count as playing :)
            return m_clientState == eClientState.Playing || m_clientState == eClientState.Linkdead;
        }
    }
    
    /// <summary>
    /// Holds the current clientstate
    /// </summary>
    protected volatile eClientState m_clientState = eClientState.NotConnected;
    
    /// <summary>
    /// The packetsender of this client
    /// </summary>
    protected IPacketLib m_packetLib;

    /// <summary>
    /// The packetreceiver of this client
    /// </summary>
    protected PacketProcessor m_packetProcessor;

    /// <summary>
    /// Holds the time of the last ping
    /// </summary>
    protected long m_pingTime = DateTime.Now.Ticks; // give ping time on creation
    
    /// <summary>
    /// This variable holds all info about the active player
    /// </summary>
    protected GamePlayer m_player;    
    /// <summary>
    /// This variable holds the sessionid
    /// </summary>
    protected int m_sessionID;
    
    /// <summary>
    /// Gets/Sets the time of last ping packet
    /// </summary>
    public long PingTime
    {
        get { return m_pingTime; }
        set { m_pingTime = value; }
    }
    
    /// <summary>
    /// Gets or sets the packet sender
    /// </summary>
    public IPacketLib Out
    {
        get { return m_packetLib; }
        set { m_packetLib = value; }
    }

    /// <summary>
    /// Gets or Sets the packet receiver
    /// </summary>
    public PacketProcessor PacketProcessor
    {
        get { return m_packetProcessor; }
        set { m_packetProcessor = value; }
    }
    
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
    /// Gets or sets the player this client is using
    /// </summary>
    public GamePlayer Player
    {
        get { return m_player; }
        set
        {
            GamePlayer oldPlayer = Interlocked.Exchange(ref m_player, value);
            if (oldPlayer != null)
            {
                oldPlayer.Delete();
            }

            GameEventMgr.Notify(GameClientEvent.PlayerLoaded, this); // hmm seems not right
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

    /// <summary>
    /// Constructor for a game client
    /// </summary>
    /// <param name="srvr">The server that's communicating with this client</param>
    public GameClient(BaseServer srvr)
        : base(srvr)
    {
        m_player = null;
    }    
}