using System;
using System.Reflection;


class Program
{
    private static Assembly LoadFromAlternativeLocation(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var altLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"lib", assemblyName);
        if (File.Exists(altLocation)) return Assembly.LoadFrom(altLocation);
        else return null;
    }
    
    [STAThread]
    static void Main(string[] args)
    {
        // Graveen: the lib path append is now specified in the .config file.
        //AppDomain.CurrentDomain.AppendPrivatePath("."+Path.DirectorySeparatorChar+"lib");
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromAlternativeLocation);

        Thread.CurrentThread.Name = "MAIN";        
        
        // 서버 시작을 알림
        Console.WriteLine($"[{DateTime.Now}] ## 서버 준비 ##");
    }
}