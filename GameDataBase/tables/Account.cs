﻿using Project.Database;
using Project.Database.Attributes;

namespace Project.DataBase;

/// <summary>
/// Account table
/// </summary>
[DataTable(TableName="Account")]
public class Account : DataObject
{
	private string m_name;
	private string m_password;
	private DateTime m_creationDate;
	private DateTime m_lastLogin;
	private int m_realm;
	private uint m_plvl;
	private int m_state;
	private String m_mail;
	private string m_lastLoginIP;
	private string m_language;
	private string m_lastClientVersion;
	private bool m_isMuted;
	
	/// <summary>
	/// Create account row in DB
	/// </summary>
	public Account()
	{
		m_name = null;
		m_password = null;
		m_creationDate = DateTime.Now;
		m_plvl = 1;
		m_realm = 0;
		m_isMuted = false;
	}

	/// <summary>
	/// The name of the account (login)
	/// </summary>
	[PrimaryKey]
	public string Name
	{
		get
		{
			return m_name;
		}
		set
		{
			Dirty = true;
			m_name = value;
		}
	}

	/// <summary>
	/// The password of this account encode in MD5 or clear when start with ##
	/// </summary>
	[DataElement(AllowDbNull=false)]
	public string Password
	{
		get
		{
			return m_password;
		}
		set
		{
			Dirty = true;
			m_password = value;
		}
	}

	/// <summary>
	/// The date of creation of this account
	/// </summary>
	[DataElement(AllowDbNull=false)]
	public DateTime CreationDate
	{
		get
		{
			return m_creationDate;
		}
		set
		{
			m_creationDate = value;
			Dirty = true;
		}
	}

	/// <summary>
	/// The date of last login of this account
	/// </summary>
	[DataElement(AllowDbNull=true)]
	public DateTime LastLogin
	{
		get
		{
			return m_lastLogin;
		}
		set
		{
			Dirty = true;
			m_lastLogin = value;
		}
	}

	/// <summary>
	/// The realm of this account
	/// </summary>
	[DataElement(AllowDbNull=false)]
	public int Realm
	{
		get
		{
			return m_realm;
		}
		set
		{
			Dirty = true;
			m_realm = value;
		}
	}

	/// <summary>
	/// The private level of this account (admin=3, GM=2 or player=1)
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public uint PrivLevel
	{
		get
		{
			return m_plvl;
		}
		set
		{
			m_plvl = value;
			Dirty = true;
		}
	}
	
	/// <summary>
	/// Status of this account
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public int Status {
		get { return m_state; }
		set { Dirty = true; m_state = value; }
	}

	/// <summary>
	/// The mail of this account
	/// </summary>
	[DataElement(AllowDbNull = true)]
	public string Mail {
		get { return m_mail; }
		set { Dirty = true; m_mail = value; }
	}

	/// <summary>
	/// The last IP logged onto this account
	/// </summary>
	[DataElement(AllowDbNull = true, Index = true)]
	public string LastLoginIP
	{
		get { return m_lastLoginIP; }
		set { m_lastLoginIP = value; }
	}

	/// <summary>
	/// The last Client Version used
	/// </summary>
	[DataElement(AllowDbNull = true)]
	public string LastClientVersion
	{
		get { return m_lastClientVersion; }
		set { m_lastClientVersion = value; }
	}

	/// <summary>
	/// The player language
	/// </summary>
	[DataElement(AllowDbNull = true)]
	public string Language
	{
		get { return m_language; }
		set { Dirty = true; m_language = value.ToUpper(); }
	}

	/// <summary>
	/// Is this account muted from public channels?
	/// </summary>
	[DataElement(AllowDbNull = false)]
	public bool IsMuted
	{
		get { return m_isMuted; }
		set { Dirty = true; m_isMuted = value; }
	}

	/// <summary>
	/// List of characters on this account
	/// </summary>
	// [Relation(LocalField = nameof( Name ), RemoteField = nameof( DOLCharacters.AccountName ), AutoLoad = true, AutoDelete=true)]
	// public DOLCharacters[] Characters;

	/// <summary>
	/// List of bans on this account
	/// </summary>
	[Relation(LocalField = nameof( Name ), RemoteField = nameof( DBBannedAccount.Account ), AutoLoad = true, AutoDelete = true)]
	public DBBannedAccount[] BannedAccount;
	
	/// <summary>
	/// List of Custom Params for this account
	/// </summary>
	[Relation(LocalField = nameof( Name ), RemoteField = nameof( AccountXCustomParam.Name ), AutoLoad = true, AutoDelete = true)]
	public AccountXCustomParam[] CustomParams;
}

/// <summary>
/// Account Custom Params linked to Account Entry
/// </summary>
[DataTable(TableName = "AccountXCustomParam")]
public class AccountXCustomParam : CustomParam
{
	private string m_name;
	
	/// <summary>
	/// Account Table Account Name Reference
	/// </summary>
	[DataElement(AllowDbNull = false, Index = true, Varchar = 255)]
	public string Name {
		get { return m_name; }
		set { Dirty = true; m_name = value; }
	}
			
	/// <summary>
	/// Create new instance of <see cref="AccountXCustomParam"/> linked to Account Name
	/// </summary>
	/// <param name="Name">Account Name</param>
	/// <param name="KeyName">Key Name</param>
	/// <param name="Value">Value</param>
	public AccountXCustomParam(string Name, string KeyName, string Value)
		: base(KeyName, Value)
	{
		this.Name = Name;
	}
	
	/// <summary>
	/// Create new instance of <see cref="AccountXCustomParam"/>
	/// </summary>
	public AccountXCustomParam()
	{
	}
}