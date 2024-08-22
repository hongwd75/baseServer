using System.Reflection;
using System.Text;
using System.Text.Json;
using log4net;
using Newtonsoft.Json;
using Project.GS.ServerProperties;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Project.GS;

public class ScriptMgr
{
    /// <summary>
    /// Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private static Dictionary<string, Assembly> m_compiledScripts = new Dictionary<string, Assembly>();
    
    /// <summary>
    /// Get an array of all script assemblies : 컴파일 스크립트가 추가되는 경우에 이곳에서 저장된다.
    /// </summary>
    public static Assembly[] Scripts
    {
        get
        {
            return m_compiledScripts.Values.ToArray();
        }
    }

    /// <summary>
    /// Get an array of GameServer Assembly with all scripts assemblies : GameServer까지 포함한 리스트를 반환한다.
    /// </summary>
    public static Assembly[] GameServerScripts
    {
        get
        {
            return m_compiledScripts.Values.Concat( new[] { typeof(GameServer).Assembly } ).ToArray();
        }
    }
    
    /// <summary>
    /// Get all loaded assemblies with Scripts Last
    /// </summary>
    public static Assembly[] AllAssembliesScriptsLast
    {
        get
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(asm => !Scripts.Contains(asm)).Concat(Scripts).ToArray();
        }
    }		    
 
    /// <summary>
    /// Parses a directory for all source files
    /// </summary>
    /// <param name="path">The root directory to start the search in</param>
    /// <param name="filter">A filter representing the types of files to search for</param>
    /// <param name="deep">True if subdirectories should be included</param>
    /// <returns>An ArrayList containing FileInfo's for all files in the path</returns>
    private static IList<FileInfo> ParseDirectory(DirectoryInfo path, string filter, bool deep)
    {
        if (!path.Exists)
            return new List<FileInfo>();
		
        return path.GetFiles(filter, SearchOption.TopDirectoryOnly).Union(deep ? path.GetDirectories().Where(di => !di.Name.Equals("obj", StringComparison.OrdinalIgnoreCase)).SelectMany(di => di.GetFiles(filter, SearchOption.AllDirectories)) : Array.Empty<FileInfo>() ).ToList();
    }
    
	/// <summary>
	/// Compiles the scripts into an assembly
	/// </summary>
	/// <param name="compileVB">True if the source files will be in VB.NET</param>
	/// <param name="scriptFolder">Path to the source files</param>
	/// <param name="outputPath">Name of the assembly to be generated</param>
	/// <param name="asm_names">References to other assemblies</param>
	/// <returns>True if succeeded</returns>
	public static bool CompileScripts(bool compileVB, string scriptFolder, string outputPath, string[] asm_names)
	{
		var outputFile = new FileInfo(outputPath);
		if (!scriptFolder.EndsWith(@"\") && !scriptFolder.EndsWith(@"/"))
			scriptFolder = scriptFolder + "/";

		//Reset the assemblies
		m_compiledScripts.Clear();

		//Check if there are any scripts, if no scripts exist, that is fine as well
		IList<FileInfo> files = ParseDirectory(new DirectoryInfo(scriptFolder), compileVB ? "*.vb" : "*.cs", true);
		if (files.Count == 0)
		{
			return true;
		}

		//Recompile is required as standard
		bool recompileRequired = true;

		//This file should hold the script infos
		var configFile = new FileInfo(outputFile.FullName + ".json");

		//If the script assembly is missing, recompile is required
		if (!outputFile.Exists)
		{
			if (log.IsDebugEnabled)
				log.Debug("Script assembly missing, recompile required!");
		}
		else
		{
			//Script assembly found, check if we have a file modify info
			if (configFile.Exists)
			{
				//Ok, we have a config file containing the script file sizes and dates
				//let's check if any script was modified since last compiling them
				if (log.IsDebugEnabled)
					log.Debug("Found script info file");

				try
				{
					string loadjsonstring = File.ReadAllText(configFile.FullName);
					CompiledScriptsData deserializedData = JsonConvert.DeserializeObject<CompiledScriptsData>(loadjsonstring);

					//Assume no scripts changed
					recompileRequired = false;
					
					if (deserializedData != null && deserializedData.Count > 0)
					{
						//Now test the files
						foreach (FileInfo finfo in files)
						{
							if (deserializedData.RemoveSameData(finfo.FullName, finfo.Length,
								    finfo.LastWriteTime.ToFileTime()) == false)
							{
								recompileRequired = true;
							}
						}

						recompileRequired |= deserializedData.Count > 0; // some compiled script was removed						
					}

					if (recompileRequired && log.IsDebugEnabled)
					{
						log.Debug("At least one file was modified, recompile required!");
					}
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error("Error during script info file to scripts compare", e);
				}
			}
			else
			{
				if (log.IsDebugEnabled)
					log.Debug("Script info file missing, recompile required!");
			}
		}
		
		//If we need no compiling, we load the existing assembly!
		if (!recompileRequired)
		{
			recompileRequired = !LoadAssembly(outputFile.FullName);
			
			if (!recompileRequired)
			{
				//Return success!
				return true;
			}
		}

		//We need a recompile, if the dll exists, delete it firsthand
		if (outputFile.Exists)
			outputFile.Delete();

		var compilationSuccessful = false;
		try
		{
			var compiler = new DOLScriptCompiler();
			if (compileVB) compiler.SetToVisualBasicNet();

			var compiledAssembly = compiler.Compile(outputFile, files);
			compilationSuccessful = true;

			AddOrReplaceAssembly(compiledAssembly);
		}
		catch (Exception e)
		{
			if (log.IsErrorEnabled)
				log.Error("CompileScripts", e);
			m_compiledScripts.Clear();
		}
		//now notify our callbacks
		if (!compilationSuccessful) return false;

		CompiledScriptsData newconfig = new CompiledScriptsData();
		foreach (var finfo in files)
		{
			newconfig.ScriptsDatasList.Add(new CompiledScriptsData.Datas()
			{
				name = finfo.FullName,
				size = finfo.Length,
				filetime = finfo.LastWriteTime.ToFileTime()
			});
		}
		if (log.IsDebugEnabled)
			log.Debug("Writing script info file");

		File.WriteAllText(configFile.FullName, JsonConvert.SerializeObject(newconfig,Formatting.Indented),Encoding.UTF8);

		return true;
	}

	
	
	/// <summary>
	/// Load an Assembly from DLL path.
	/// </summary>
	/// <param name="dllName">path to Assembly DLL File</param>
	/// <returns>True if assembly is loaded</returns>
	public static bool LoadAssembly(string dllName)
	{
		try
		{
			Assembly asm = Assembly.LoadFrom(dllName);
			ScriptMgr.AddOrReplaceAssembly(asm);

			if (log.IsInfoEnabled)
				log.InfoFormat("Assembly {0} loaded successfully from path {1}", asm.FullName, dllName);
			
			return true;
		}
		catch (Exception e)
		{
			if (log.IsErrorEnabled)
				log.ErrorFormat("Error loading Assembly from path {0} - {1}", dllName, e);
		}
		
		return false;
	}

	/// <summary>
	/// Add or replace an assembly in the collection of compiled assemblies
	/// </summary>
	/// <param name="assembly"></param>
	public static void AddOrReplaceAssembly(Assembly assembly)
	{
		if (m_compiledScripts.ContainsKey(assembly.FullName))
		{
			m_compiledScripts[assembly.FullName] = assembly;
			if (log.IsDebugEnabled)
				log.Debug("Replaced assembly " + assembly.FullName);
		}
		else
		{
			m_compiledScripts.Add(assembly.FullName, assembly);
		}
	}

	/// <summary>
	/// Removes an assembly from the game servers list of usable assemblies
	/// </summary>
	/// <param name="fullName"></param>
	public static bool RemoveAssembly(string fullName)
	{
		if (m_compiledScripts.ContainsKey(fullName))
		{
			m_compiledScripts.Remove(fullName);
			return true;
		}

		return false;
	}

	/// <summary>
	/// searches the given assembly for AbilityActionHandlers
	/// </summary>
	/// <param name="asm">The assembly to search through</param>
	/// <returns>Hashmap consisting of keyName => AbilityActionHandler Type</returns>
	public static IList<KeyValuePair<string, Type>> FindAllAbilityActionHandler(Assembly asm)
	{
		List<KeyValuePair<string, Type>> abHandler = new List<KeyValuePair<string, Type>>();
		if (asm != null)
		{
			foreach (Type type in asm.GetTypes())
			{
				if (!type.IsClass)
					continue;
				if (type.GetInterface("DOL.GS.IAbilityActionHandler") == null)
					continue;
				if (type.IsAbstract)
					continue;

				// 아직 사용하지 않음
				// object[] objs = type.GetCustomAttributes(typeof(SkillHandlerAttribute), false);
				// for (int i = 0; i < objs.Length; i++)
				// {
				// 	if (objs[i] is SkillHandlerAttribute)
				// 	{
				// 		SkillHandlerAttribute attr = objs[i] as SkillHandlerAttribute;
				// 		abHandler.Add(new KeyValuePair<string, Type>(attr.KeyName, type));
				// 	}
				// }
			}
		}
		return abHandler;
	}

	/// <summary>
	/// Search for a type by name; first in GameServer assembly then in scripts assemblies
	/// </summary>
	/// <param name="name">The type name</param>
	/// <returns>Found type or null</returns>
	public static Type GetType(string name)
	{
		Type t = typeof(GameServer).Assembly.GetType(name);
		if (t == null)
		{
			foreach (Assembly asm in Scripts)
			{
				t = asm.GetType(name);
				if (t == null) continue;
				return t;
			}
		}
		else
		{
			return t;
		}
		return null;
	}

	/// <summary>
	/// Finds all classes that derive from given type.
	/// First check scripts then GameServer assembly.
	/// </summary>
	/// <param name="baseType">The base class type.</param>
	/// <returns>Array of types or empty array</returns>
	public static Type[] GetDerivedClasses(Type baseType)
	{
		if (baseType == null)
			return Array.Empty<Type>();

		List<Type> types = new List<Type>();

		foreach (Assembly asm in GameServerScripts)
		{
			foreach (Type t in asm.GetTypes())
			{
				if (t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t))
					types.Add(t);
			}
		}

		return types.ToArray();
	}
	
	/// <summary>
	/// Create new instance of ClassType, Looking through Assemblies and Scripts with given param
	/// </summary>
	/// <param name="classType"></param>
	/// <param name="args"></param>
	/// <returns></returns>
	public static C CreateObjectFromClassType<C, T>(string classType, T args)
		where C : class
	{
		foreach (Assembly assembly in AllAssembliesScriptsLast)
		{
			try
			{
				C instance = assembly.CreateInstance(classType, false, BindingFlags.CreateInstance, null, new object[] { args }, null, null) as C;
				if (instance != null)
					return instance;
			}
			catch (Exception)
			{
			}

		}
		
		return null;
	}
}