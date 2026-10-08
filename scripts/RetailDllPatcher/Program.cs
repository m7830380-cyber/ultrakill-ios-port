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
var managedSrc = Path.GetFullPath(Path.Combine(dllDir, "..", "..", "..", "unity-ios", "Assets", "Plugins", "UltrakillManaged"));
if (Directory.Exists(managedSrc))
{
    resolver.AddSearchDirectory(managedSrc);
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
var pathCombine = module.ImportReference(
    typeof(Path).GetMethod(nameof(Path.Combine), new[] { typeof(string), typeof(string) })!);

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
