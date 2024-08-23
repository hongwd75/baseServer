using System.Globalization;
using System.Reflection;
using log4net;
using Project.DataBase.Tables;
using Project.ServerProperties;

namespace Project.GS.ServerProperties;

public class Properties
{
    /// <summary>
    /// Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    #region  서버 설정
    [ServerProperty("system", "enable_debug", "디버그 on / off", false)]
    public static bool ENABLE_DEBUG;
    /// <summary>
    /// Whether to use the sync timer utility or not
    /// </summary>
    [ServerProperty("system", "use_sync_timer", "Shall we use the sync timers utility?", true)]
    public static bool USE_SYNC_UTILITY;

    /// <summary>
    /// Ignore too long outcoming packet or not
    /// </summary>
    [ServerProperty("system", "ignore_too_long_outcoming_packet", "Shall we ignore too long outcoming packet ?", false)]
    public static bool IGNORE_TOO_LONG_OUTCOMING_PACKET;
    
    /// <summary>
    /// If the server should only accept connections from staff
    /// </summary>
    [ServerProperty("system", "staff_login", "Staff Login Only - 관계자들만 접속 가능하게 설정. ones allowed to Log in values True,False", false)]
    public static bool STAFF_LOGIN;

    /// <summary>
    /// The minimum client version required to connect
    /// </summary>
    [ServerProperty("system", "client_version_min", "Minimum Client Version - Edit this to change which client version at the least have to be used: -1 = any, 1.80 = 180", -1)]
    public static int CLIENT_VERSION_MIN;

    /// <summary>
    /// What is the maximum client type allowed to connect
    /// </summary>
    [ServerProperty("system", "client_type_max", "What is the maximum client type allowed to connect", -1)]
    public static int CLIENT_TYPE_MAX;

    /// <summary>
    /// The max number of players on the server
    /// </summary>
    [ServerProperty("system", "max_players", "Max Players - Edit this to set the maximum players allowed to connect at the same time set 0 for unlimited", 1000)]
    public static int MAX_PLAYERS;    

    /// <summary>
    /// Whether or not to enable the audit log
    /// </summary>
    [ServerProperty("system", "enable_audit_log", "Whether or not to enable the audit log", false)]
    public static bool ENABLE_AUDIT_LOG;    
    
    /// <summary>
    /// Anon Modifier
    /// </summary>
    [ServerProperty("server", "anon_modifier", "Various modifying options for anon, 0 = default, 1 = /who shows player but as ANON, -1 = disabled", 0)]
    public static int ANON_MODIFIER;    
    #endregion
    
    
    /// <summary>
    /// Init the properties
    /// </summary>
    public static void InitProperties()
    {
        var propDict = AllDomainProperties;
        foreach (var prop in propDict)
        {
            Load(prop.Value.Item1, prop.Value.Item2, prop.Value.Item3);
        }
			
        // Refresh static dict values for display
        AllCurrentProperties = propDict.ToDictionary(k => k.Key, v => v.Value.Item2.GetValue(null));
    }
    
    
    // 내부 코드들
	public static IDictionary<string, object> AllCurrentProperties
	{
		get; private set;
	}
	
	/// <summary>
	/// Get a Dictionary that tracks all Properties by Key String
	/// Returns the ServerPropertyAttribute, the Static Field with current Value, and the according DataObject
	/// Create a default dataObject if value wasn't found in Database
	/// </summary>
	public static IDictionary<string, Tuple<ServerPropertyAttribute, FieldInfo, ServerProperty>> AllDomainProperties
	{
		get
		{
			var result = new Dictionary<string, Tuple<ServerPropertyAttribute, FieldInfo, ServerProperty>>();
			var allProperties = GameServer.Database.SelectAllObjects<ServerProperty>();
			
			foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (Type type in asm.GetTypes())
				{
					foreach (FieldInfo field in type.GetFields())
					{
						// Properties are Static
						if (!field.IsStatic)
							continue;
						
						// Properties shoud contain a property attribute
						object[] attribs = field.GetCustomAttributes(typeof(ServerPropertyAttribute), false);
						if (attribs.Length == 0)
							continue;
						
						ServerPropertyAttribute att = (ServerPropertyAttribute)attribs[0];
						
						ServerProperty serverProp = allProperties.Where(p => p.Key.Equals(att.Key, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
						
						if (serverProp == null)
						{
							// Init DB Object
							serverProp = new ServerProperty();
							serverProp.Category = att.Category;
							serverProp.Key = att.Key;
							serverProp.Description = att.Description;
							if (att.DefaultValue is double)
							{
								CultureInfo myCIintl = new CultureInfo("en-US", false);
								IFormatProvider provider = myCIintl.NumberFormat;
								serverProp.DefaultValue = ((double)att.DefaultValue).ToString(provider);
							}
							else
							{
								serverProp.DefaultValue = att.DefaultValue.ToString();
							}
							serverProp.Value = serverProp.DefaultValue;
						}
						
						result[att.Key] = new Tuple<ServerPropertyAttribute, FieldInfo, ServerProperty>(att, field, serverProp);
					}
				}
			}
			return result;
		}
	}
	
	/// <summary>
	/// This method loads the property from the database and returns
	/// the value of the property as strongly typed object based on the
	/// type of the default value
	/// </summary>
	/// <param name="attrib">The attribute</param>
	/// <returns>The real property value</returns>
	public static void Load(ServerPropertyAttribute attrib, FieldInfo field, ServerProperty prop)
	{
		string key = attrib.Key;
			
		// Not Added to database...
		if (!prop.IsPersisted)
		{
			GameServer.Database.AddObject(prop);
			log.DebugFormat("Cannot find server property {0} creating it", key);
		}
			
		log.DebugFormat("Loading {0} Value is {1}", key, prop.Value);
			
		try
		{
			if (field.IsInitOnly)
				log.WarnFormat("Property {0} is ReadOnly, Value won't be changed - {1} !", key, field.GetValue(null));
				
			//we do this because we need "1.0" to be considered double sometimes its "1,0" in other countries
			CultureInfo myCIintl = new CultureInfo("en-US", false);
			IFormatProvider provider = myCIintl.NumberFormat;
			field.SetValue(null, Convert.ChangeType(prop.Value, attrib.DefaultValue.GetType(), provider));
		}
		catch (Exception e)
		{
			log.ErrorFormat("Exception in ServerProperties Load: {0}", e);
			log.ErrorFormat("Trying to load {0} value is {1}", key, prop.Value);
		}
	}
		
	/// <summary>
	/// Refreshes the server properties from the DB
	/// </summary>
	public static void Refresh()
	{
		log.Info("Refreshing server properties...");
		InitProperties();
	}	
}