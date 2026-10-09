using Mono.Cecil;
using Mono.Cecil.Cil;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: RetailDllPatcher <Assembly-CSharp.dll> [extra-search-dirs...]");
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

PatchPrefsPath(module);
PatchSceneHelperOnSceneLoaded(module);
PatchSceneHelperIsSceneRankless(module);
// BloodsplatterManager.Start throws on null bloodCompositeShader, leaving NativeArrays
// unallocated; BloodstainParent.Start then SIGSEGVs in CreateParent (white screen crash).
StubMethodEmpty(module, "BloodstainParent", "Start");
StubMethodEmpty(module, "BloodsplatterManager", "Start");
StubMethodReturnInt(module, "BloodsplatterManager", "CreateParent", 0);
// Portal system uses uninitialized NativeLists → SIGSEGV in PortalAwareRenderer.Think.
StubMethodEmpty(module, "ULTRAKILL.Portal.PortalAwareRenderer", "LateUpdate");
StubMethodEmpty(module, "ULTRAKILL.Portal.PortalAwareRenderer", "Think");
StubMethodEmpty(module, "ULTRAKILL.Portal.PortalManagerV2", "Update");
StubMethodEmpty(module, "ULTRAKILL.Portal.PortalManagerV2", "FixedUpdate");
StubMethodEmpty(module, "ULTRAKILL.Portal.PortalManagerV2", "LateUpdate");

var tempPath = dllPath + ".patched";
asm.Write(tempPath);
File.Copy(tempPath, dllPath, true);
File.Delete(tempPath);
Console.WriteLine("RetailDllPatcher: done");
return 0;

static void PatchPrefsPath(ModuleDefinition module)
{
    var prefsType = module.GetType("PrefsManager")
        ?? throw new Exception("PrefsManager type not found.");

    var getter = prefsType.Properties.FirstOrDefault(p => p.Name == "PrefsPath")?.GetMethod;
    if (getter == null || !getter.IsStatic)
    {
        throw new Exception("PrefsManager.PrefsPath getter not found.");
    }

    var coreRef = module.AssemblyReferences.FirstOrDefault(r => r.Name == "UnityEngine.CoreModule")
        ?? throw new Exception("UnityEngine.CoreModule reference missing.");

    var appType = new TypeReference("UnityEngine", "Application", module, coreRef);
    var persistentGetter = new MethodReference("get_persistentDataPath", module.TypeSystem.String, appType)
    {
        HasThis = false,
    };

    var mscorlib = module.TypeSystem.String.Scope;
    var pathType = new TypeReference("System.IO", "Path", module, mscorlib);
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
    Console.WriteLine("Patched PrefsManager.PrefsPath -> persistentDataPath/Preferences");
}

static void PatchSceneHelperOnSceneLoaded(ModuleDefinition module)
{
    var sh = module.GetType("SceneHelper");
    if (sh == null)
    {
        Console.WriteLine("WARN: SceneHelper not found");
        return;
    }

    var method = sh.Methods.FirstOrDefault(m => m.Name == "OnSceneLoaded" && m.HasBody);
    var eventSystemField = sh.Fields.FirstOrDefault(f => f.Name == "eventSystem");
    if (method == null || eventSystemField == null)
    {
        Console.WriteLine("WARN: OnSceneLoaded/eventSystem missing");
        return;
    }

    var body = method.Body;
    var il = body.GetILProcessor();
    var insts = body.Instructions.ToList();

    // Idempotent: already patched if method starts with ldarg.0; ldfld eventSystem; brfalse
    if (insts.Count >= 3
        && insts[0].OpCode == OpCodes.Ldarg_0
        && insts[1].OpCode == OpCodes.Ldfld
        && insts[1].Operand is FieldReference alreadyFr
        && alreadyFr.Name == "eventSystem"
        && insts[2].OpCode == OpCodes.Brfalse)
    {
        Console.WriteLine("SceneHelper.OnSceneLoaded already patched; skip");
        return;
    }

    // Guard Instantiate(this.eventSystem): insert null check that branches past the call.
    for (var i = 0; i < insts.Count - 2; i++)
    {
        var a = insts[i];
        var b = insts[i + 1];
        var c = insts[i + 2];
        if (a.OpCode != OpCodes.Ldarg_0)
        {
            continue;
        }

        if (b.OpCode != OpCodes.Ldfld || b.Operand is not FieldReference fr || fr.Name != "eventSystem")
        {
            continue;
        }

        if ((c.OpCode != OpCodes.Call && c.OpCode != OpCodes.Callvirt)
            || c.Operand is not MethodReference mr
            || mr.Name != "Instantiate")
        {
            continue;
        }

        var after = c.Next ?? throw new Exception("Instantiate has no next instruction");

        // Also skip the Find/Destroy EventSystem block when prefab is null:
        // jump from method start to after Instantiate when eventSystem == null.
        var start = body.Instructions[0];
        var skipAllEs = il.Create(OpCodes.Ldarg_0);
        var skipLdfld = il.Create(OpCodes.Ldfld, eventSystemField);
        var skipBr = il.Create(OpCodes.Brfalse, after);
        il.InsertBefore(start, skipAllEs);
        il.InsertBefore(start, skipLdfld);
        il.InsertBefore(start, skipBr);

        Console.WriteLine("Patched SceneHelper.OnSceneLoaded: skip EventSystem Destroy/Instantiate when prefab null");
        return;
    }

    Console.WriteLine("WARN: Instantiate(eventSystem) pattern not found");
}

static void PatchSceneHelperIsSceneRankless(ModuleDefinition module)
{
    var sh = module.GetType("SceneHelper");
    var getter = sh?.Properties.FirstOrDefault(p => p.Name == "IsSceneRankless")?.GetMethod;
    if (getter == null || !getter.HasBody)
    {
        return;
    }

    // Always return false — avoids NRE when embeddedSceneInfo is null.
    getter.Body.Instructions.Clear();
    getter.Body.Variables.Clear();
    getter.Body.ExceptionHandlers.Clear();
    var il = getter.Body.GetILProcessor();
    il.Append(il.Create(OpCodes.Ldc_I4_0));
    il.Append(il.Create(OpCodes.Ret));
    Console.WriteLine("Patched SceneHelper.IsSceneRankless -> always false (null-safe)");
}

static void StubMethodEmpty(ModuleDefinition module, string typeName, string methodName)
{
    var type = module.GetType(typeName);
    // Think(bool) has an optional parameter — match by name only.
    var method = type?.Methods.FirstOrDefault(m => m.Name == methodName && m.HasBody
        && (methodName != "Think" || m.Parameters.Count <= 1)
        && (methodName == "Think" || !m.HasParameters || m.Parameters.All(p => p.HasDefault)));
    if (method == null)
    {
        method = type?.Methods.FirstOrDefault(m => m.Name == methodName && m.HasBody);
    }

    if (method == null)
    {
        Console.WriteLine($"WARN: {typeName}.{methodName} not found for stub");
        return;
    }

    // Idempotent: already empty stub
    if (method.Body.Instructions.Count == 1 && method.Body.Instructions[0].OpCode == OpCodes.Ret)
    {
        Console.WriteLine($"{typeName}.{methodName} already stubbed; skip");
        return;
    }

    method.Body.Instructions.Clear();
    method.Body.Variables.Clear();
    method.Body.ExceptionHandlers.Clear();
    method.Body.GetILProcessor().Append(Instruction.Create(OpCodes.Ret));
    Console.WriteLine($"Stubbed {typeName}.{methodName} -> empty (iOS crash guard)");
}

static void StubMethodReturnInt(ModuleDefinition module, string typeName, string methodName, int value)
{
    var type = module.GetType(typeName);
    var method = type?.Methods.FirstOrDefault(m => m.Name == methodName && m.HasBody && m.ReturnType.FullName == "System.Int32");
    if (method == null)
    {
        Console.WriteLine($"WARN: {typeName}.{methodName} (int) not found for stub");
        return;
    }

    var insts = method.Body.Instructions;
    if (insts.Count == 2 && insts[1].OpCode == OpCodes.Ret
        && (insts[0].OpCode == OpCodes.Ldc_I4_0 || insts[0].OpCode == OpCodes.Ldc_I4))
    {
        Console.WriteLine($"{typeName}.{methodName} already stubbed; skip");
        return;
    }

    method.Body.Instructions.Clear();
    method.Body.Variables.Clear();
    method.Body.ExceptionHandlers.Clear();
    var il = method.Body.GetILProcessor();
    il.Append(value == 0 ? il.Create(OpCodes.Ldc_I4_0) : il.Create(OpCodes.Ldc_I4, value));
    il.Append(il.Create(OpCodes.Ret));
    Console.WriteLine($"Stubbed {typeName}.{methodName} -> return {value} (iOS crash guard)");
}
