using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Project.GS;

public class DOLScriptCompiler
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private Compilation compiler;
    private Microsoft.CodeAnalysis.Emit.EmitResult lastEmitResult;
    private static List<PortableExecutableReference> referencedAssemblies;

    static DOLScriptCompiler()
    {
        LoadDefaultAssemblies();
    }

    public bool HasErrors => !lastEmitResult.Success;

    public void SetToVisualBasicNet()
    {
        throw new NotSupportedException("Please migrate your scripts to C#.");
    }

    public Assembly Compile(FileInfo outputFile, IEnumerable<FileInfo> sourceFiles)
    {
        var syntaxTrees = sourceFiles.Where(file => file.Name != "AssemblyInfo.cs")
            .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file.FullName)));

        Directory.CreateDirectory(outputFile.DirectoryName);
        Compile(outputFile, syntaxTrees);

        if (HasErrors)
        {
            PrintErrorMessagesToConsole();
            throw new ApplicationException("Scripts compilation was unsuccessful. Abort startup!");
        }
        referencedAssemblies.Add(GetPortableExecutableReference(outputFile.Name));
        return Assembly.LoadFrom(outputFile.FullName);
    }

    public Assembly CompileFromText(GameClient client, string code)
    {
        var outputFile = new FileInfo("code_"+Guid.NewGuid()+".dll");
        var syntaxTrees = new List<SyntaxTree>() { CSharpSyntaxTree.ParseText(code) };

        Compile(outputFile, syntaxTrees);

        if (HasErrors)
        {
            PrintErrorMessagesTo(client);
            File.Delete(outputFile.FullName);
            return null;
        }
        var assembly = Assembly.Load(File.ReadAllBytes(outputFile.FullName));
        File.Delete(outputFile.FullName);
        return assembly;
    }

    private void Compile(FileInfo outputFile, IEnumerable<SyntaxTree> syntaxTrees)
    {
        var compilerParameters = new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                warningLevel: 2);
        compiler = CSharpCompilation.Create(
            outputFile.Name,
            options: compilerParameters,
            references: referencedAssemblies,
            syntaxTrees: syntaxTrees);
        var emitResult = compiler.Emit(outputFile.FullName);
        GC.Collect();

        lastEmitResult = emitResult;
    }

    private void PrintErrorMessagesToConsole()
    {
        foreach (var diag in ErrorDiagnostics)
        {
            log.Error($"\t{diag.Location} {diag.Id}: {diag.GetMessage()}");
        }
    }

    private void PrintErrorMessagesTo(GameClient client)
    {
        // if (client.Player != null)
        // {
        //     client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "AdminCommands.Code.ErrorCompiling"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
        //
        //     foreach (var diag in ErrorDiagnostics)
        //         client.Out.SendMessage(diag.GetMessage(), eChatType.CT_System, eChatLoc.CL_PopupWindow);
        // }
        // else
        {
            log.Debug("Error compiling code.");
        }
    }

    private IEnumerable<Diagnostic> ErrorDiagnostics
        => lastEmitResult.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

    private static void LoadDefaultAssemblies()
    {
        var currentDomainReferences = AppDomain.CurrentDomain
                            .GetAssemblies()
                            .Where(a => !a.IsDynamic)
                            .Select(a => a.Location)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => MetadataReference.CreateFromFile(s));
        var additionalReferences = new string[] { 
            "System.Security.Cryptography", //for SHA256 in AutoXMLDatabaseUpdate
            "System.Net.Http"
        }.Union(GameServer.Instance.Configuration.AdditionalScriptAssemblies)
            .Select(r => GetPortableExecutableReference(r));
        
        referencedAssemblies = currentDomainReferences.Union(additionalReferences).ToList();
    }

    private static PortableExecutableReference GetPortableExecutableReference(string referenceName)
    {
        var dllName = referenceName.EndsWith(".dll") ? referenceName : referenceName + ".dll";
        var probingPaths = new[] { ".", "lib", Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location) };
        var dllPath = probingPaths.Select(path => Path.Combine(path, dllName))
            .Where(fullPath => File.Exists(fullPath)).FirstOrDefault();
        if(dllPath == null)
        {
            log.Error($"Reference {referenceName} not found.");
            return null;
        }

        return MetadataReference.CreateFromFile(dllPath);
    }
}