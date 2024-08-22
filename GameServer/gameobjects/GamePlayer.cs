using Project.DataBase;
using Project.GS.PacketHandler;

namespace Project.GS;

/// <summary>
/// This class represents a player inside the game
/// </summary>
public class GamePlayer : GameLiving
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private readonly object m_LockObject = new object();

    /// <summary>
    /// This is our gameclient!
    /// </summary>
    protected readonly GameClient m_client;
		
    /// <summary>
    /// This holds the character this player is
    /// based on!
    /// (renamed and private, cause if derive is needed overwrite PlayerCharacter)
    /// </summary>
    protected DOLCharacters m_dbCharacter;
    
    /// <summary>
    /// Has this player entered the game, will be
    /// true after the first time the char enters
    /// the world
    /// </summary>
    protected bool m_enteredGame;
    
    /// <summary>
    /// Property for the optional away from keyboard message.
    /// </summary>
    public static readonly string AFK_MESSAGE = "afk_message";

    /// <summary>
    /// Property for the optional away from keyboard message.
    /// </summary>
    public static readonly string QUICK_CAST_CHANGE_TICK = "quick_cast_change_tick";

    /// <summary>
    /// Last spell cast from a used item
    /// </summary>
    public static readonly string LAST_USED_ITEM_SPELL = "last_used_item_spell";    
    
    
    /// <summary>
    /// Returns the GameClient of this Player
    /// </summary>
    public virtual GameClient Client
    {
        get { return m_client; }
    }

    /// <summary>
    /// Returns the PacketSender for this player
    /// </summary>
    public virtual IPacketLib Out
    {
        get { return Client.Out; }
    }

    /// <summary>
    /// The character the player is based on
    /// </summary>
    internal DOLCharacters DBCharacter
    {
        get { return m_dbCharacter; }
    }

    /// <summary>
    /// Has this player entered the game for the first
    /// time after logging on (not Zoning!)
    /// </summary>
    public bool EnteredGame
    {
        get { return m_enteredGame; }
        set { m_enteredGame = value; }
    }
    
    /// <summary>
    /// Marks this player as deleted
    /// </summary>
    public override void Delete()
    {			
        // // do some Cleanup
        // CleanupOnDisconnect();
			     //
        // if (Group != null)
        // {
        //     Group.RemoveMember(this);
        // }
        // BattleGroup mybattlegroup = (BattleGroup)this.TempProperties.getProperty<object>(BattleGroup.BATTLEGROUP_PROPERTY, null);
        // if (mybattlegroup != null)
        // {
        //     mybattlegroup.RemoveBattlePlayer(this);
        // }
        // if (m_guild != null)
        // {
        //     m_guild.RemoveOnlineMember(this);
        // }
        // GroupMgr.RemovePlayerLooking(this);
        // if (log.IsDebugEnabled)
        // {
        //     log.DebugFormat("({0}) player.Delete()", Name);
        // }
        base.Delete();
    }    
}