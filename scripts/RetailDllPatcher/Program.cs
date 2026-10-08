using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: RetailDllPatcher <Assembly-CSharp.dll>");
    return 1;
}

var dllPath = Path.GetFullPath(args[0]);
if (!File.Exists(dllPath))
{
    Console.Error.WriteLine("Missing: " + dllPath);
    return 1;
}

var resolver = new DefaultAssemblyResolver();
var dllDir = Path.GetDirectoryName(dllPath)!;
resolver.AddSearchDirectory(dllDir);
for (var i = 1; i < args.Length; i++)
{
    var dir = Path.GetFullPath(args[i]);
    if (Directory.Exists(dir))
    {
        resolver.AddSearchDirectory(dir);
    }
}

var readerParams = new ReaderParameters
{
    AssemblyResolver = resolver,
    ReadWrite = false,
    InMemory = true,
};

using var asm = AssemblyDefinition.ReadAssembly(dllPath, readerParams);
var module = asm.MainModule;
var prefsType = module.GetType("PrefsManager");
if (prefsType == null)
{
    Console.Error.WriteLine("PrefsManager type not found.");
    return 1;
}

var getter = prefsType.Properties.FirstOrDefault(p => p.Name == "PrefsPath")?.GetMethod;
if (getter == null || !getter.IsStatic)
{
    Console.Error.WriteLine("PrefsManager.PrefsPath getter not found.");
    return 1;
}

var coreRef = module.AssemblyReferences.FirstOrDefault(r => r.Name == "UnityEngine.CoreModule");
if (coreRef == null)
{
    Console.Error.WriteLine("UnityEngine.CoreModule reference missing from Assembly-CSharp.");
    return 1;
}

var appType = new TypeReference("UnityEngine", "Application", module, coreRef);
var persistentGetter = new MethodReference("get_persistentDataPath", module.TypeSystem.String, appType)
{
    HasThis = false,
};
var systemRef = module.AssemblyReferences.FirstOrDefault(r =>
    r.Name is "mscorlib" or "netstandard" or "System.Runtime");
if (systemRef == null)
{
    Console.Error.WriteLine("No mscorlib/netstandard reference in Assembly-CSharp.");
    return 1;
}

var pathType = new TypeReference("System.IO", "Path", module, systemRef);
var pathCombine = new MethodReference("Combine", module.TypeSystem.String, pathType)
{
    HasThis = false,
};
pathCombine.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));
pathCombine.Parameters.Add(new ParameterDefinition(module.TypeSystem.String));

getter.Body.Instructions.Clear();
getter.Body.Variables.Clear();
getter.Body.ExceptionHandlers.Clear();
var il = getter.Body.GetILProcessor();
il.Append(il.Create(OpCodes.Call, persistentGetter));
il.Append(il.Create(OpCodes.Ldstr, "Preferences"));
il.Append(il.Create(OpCodes.Call, pathCombine));
il.Append(il.Create(OpCodes.Ret));

var tempPath = dllPath + ".patched";
asm.Write(tempPath);
File.Copy(tempPath, dllPath, true);
File.Delete(tempPath);
Console.WriteLine("Patched PrefsManager.PrefsPath -> persistentDataPath/Preferences");
return 0;
