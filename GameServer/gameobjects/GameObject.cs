using Project.Database;
using Project.GS.Events;

namespace Project.GS;

/// <summary>
/// This class holds all information that
/// EVERY object in the game world needs!
/// </summary>
public abstract class GameObject
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	#region State/Random/Type

	/// <summary>
	/// Holds the current state of the object
	/// </summary>
	public enum eObjectState : byte
	{
		/// <summary>
		/// Active, visibly in world
		/// </summary>
		Active,
		/// <summary>
		/// Inactive, currently being moved or stuff
		/// </summary>
		Inactive,
		/// <summary>
		/// Deleted, waiting to be cleaned up
		/// </summary>
		Deleted
	}

	/// <summary>
	/// The Object's state! This is needed because
	/// when we remove an object it isn't instantly
	/// deleted but the state is merely set to "Deleted"
	/// This prevents the object from vanishing when
	/// there still might be enumerations running over it.
	/// A timer will collect the deleted objects and free
	/// them at certain intervals.
	/// </summary>
	protected volatile eObjectState m_ObjectState;

	/// <summary>
	/// Returns the current state of the object.
	/// Object's with state "Deleted" should not be used!
	/// </summary>
	public virtual eObjectState ObjectState
	{
		get { return m_ObjectState; }
		set
		{
			if (log.IsDebugEnabled)
				log.Debug("ObjectState: OID" + ObjectID + " " + Name + " " + m_ObjectState + " => " + value);
			m_ObjectState = value;
		}
	}
	#endregion
	
	

	protected string m_ownerID;

	/// <summary>
	/// Gets or sets the owner ID for this object
	/// </summary>
	public virtual string OwnerID
	{
		get { return m_ownerID; }
		set
		{
			m_ownerID = value;
		}
	}

	#region IDs/Database

	/// <summary>
	/// True if this object is saved in the DB
	/// </summary>
	protected bool m_saveInDB;

	/// <summary>
	/// The objectID. This is -1 as long as the object is not added to a region!
	/// </summary>
	protected int m_ObjectID = -1;

	/// <summary>
	/// The internalID. This is the unique ID of the object in the DB!
	/// </summary>
	protected string m_InternalID;

	/// <summary>
	/// Gets or Sets the current ObjectID of the Object
	/// This is done automatically by the Region and should
	/// not be done manually!!!
	/// </summary>
	public int ObjectID
	{
		get { return m_ObjectID; }
		set
		{
			if (log.IsDebugEnabled)
				log.Debug("ObjectID: " + Name + " " + m_ObjectID + " => " + value);
			m_ObjectID = value;
		}
	}

	/// <summary>
	/// Gets or Sets the internal ID (DB ID) of the Object
	/// </summary>
	public virtual string InternalID
	{
		get { return m_InternalID; }
		set { m_InternalID = value; }
	}

	/// <summary>
	/// Sets the state for this object on whether or not it is saved in the database
	/// </summary>
	public bool SaveInDB
	{
		get { return m_saveInDB; }
		set { m_saveInDB = value; }
	}

	/// <summary>
	/// Saves an object into the database
	/// </summary>
	public virtual void SaveIntoDatabase()
	{
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="obj"></param>
	public virtual void LoadFromDatabase(DataObject obj)
	{
		InternalID = obj.ObjectId;
	}

	/// <summary>
	/// Deletes a character from the DB
	/// </summary>
	public virtual void DeleteFromDatabase()
	{
	}

	/// <summary>
	/// Marks this object as deleted!
	/// </summary>
	public virtual void Delete()
	{
		Notify(GameObjectEvent.Delete, this);
		RemoveFromWorld();
		ObjectState = eObjectState.Deleted;
		GameEventMgr.RemoveAllHandlersForObject(this);
	}
	#endregion
	
	/// <summary>
	/// Removes the item from the world
	/// </summary>
	public virtual bool RemoveFromWorld()
	{
		// 대부분 코드 지움 
		if (ObjectState != eObjectState.Active)
			return false;
		Notify(GameObjectEvent.RemoveFromWorld, this);
		ObjectState = eObjectState.Inactive;
		return true;
	}	
	
	#region Notify

	public virtual void Notify(DOLEvent e, object sender, EventArgs args)
	{
		GameEventMgr.Notify(e, sender, args);
	}

	public virtual void Notify(DOLEvent e, object sender)
	{
		Notify(e, sender, null);
	}

	public virtual void Notify(DOLEvent e)
	{
		Notify(e, null, null);
	}

	public virtual void Notify(DOLEvent e, EventArgs args)
	{
		Notify(e, null, args);
	}

	#endregion	
	
	#region Level/Name/Model/GetName/GetPronoun/GetExamineMessage

	/// <summary>
	/// The level of the Object
	/// </summary>
	protected byte m_level = 0; // Default to 0 to force AutoSetStats() to be called when level set

	/// <summary>
	/// The name of the Object
	/// </summary>
	protected string m_name;
	
	/// <summary>
	/// The guild name of the Object
	/// </summary>
	protected string m_guildName;

	/// <summary>
	/// The model of the Object
	/// </summary>
	protected ushort m_model;
	
	
	/// <summary>
	/// Gets or Sets the current level of the Object
	/// </summary>
	public virtual byte Level
	{
		get { return m_level; }
		set { m_level = value; }
	}

	/// <summary>
	/// Gets or Sets the effective level of the Object
	/// </summary>
	public virtual int EffectiveLevel
	{
		get { return Level; }
		set { }
	}

	/// <summary>
	/// What level is displayed to another player
	/// </summary>
	public virtual byte GetDisplayLevel(GamePlayer player)
	{
		return Level;
	}

	/// <summary>
	/// Gets or Sets the current Name of the Object
	/// </summary>
	public virtual string Name
	{
		get { return m_name; }
		set { m_name = value; }
	}

	public virtual string GuildName
	{
		get { return m_guildName; }
		set { m_guildName = value; }
	}

	/// <summary>
	/// Gets or Sets the current Model of the Object
	/// </summary>
	public virtual ushort Model
	{
		get { return m_model; }
		set { m_model = value; }
	}
	#endregion
	
}