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
// CameraController Awake/Start NREs on null Prefs/mixers → black screen.
StubMethodEmpty(module, "CameraController", "Awake");
StubMethodEmpty(module, "CameraController", "Start");
// StyleHUD.Awake NREs on rankImage → meter child never ComboOver()'d, stays on screen.
StubMethodEmpty(module, "StyleHUD", "Awake");
StubMethodEmpty(module, "StyleHUD", "Start"); // ComboOver + gc.allWeapons — Start NREs after Awake stub
StubMethodEmpty(module, "StyleHUD", "Update");
// IntroTextController.Start freezes Rigidbody gravity → white void + stuck mid-air.
StubMethodEmpty(module, "IntroTextController", "Awake");
StubMethodEmpty(module, "IntroTextController", "Start");
StubMethodEmpty(module, "IntroTextController", "Update");
// NewMovement.Awake/Start NRE on null gc / TimeController / hud refs.
// Runtime PlayerGameplayRepair wires refs + activated/gravity.
StubMethodEmpty(module, "NewMovement", "Awake");
StubMethodEmpty(module, "NewMovement", "Start");
// Update NREs on null windStateParticle — UltrakillIOS drives move instead.
StubMethodEmpty(module, "NewMovement", "Update");
StubMethodEmpty(module, "NewMovement", "FixedUpdate");
// LateUpdate NREs on null player/opm — UltrakillIOS drives look instead.
StubMethodEmpty(module, "CameraController", "LateUpdate");
StubMethodEmpty(module, "CameraController", "Update");
// ClimbStep.OnCollisionStay NRE spam (1000+/session) while player scrapes geometry.
StubMethodEmpty(module, "ClimbStep", "OnCollisionStay");
StubMethodEmpty(module, "ClimbStep", "HandleCollision");
// Null AudioMixer assets → UpdateSFXVolume / FixedUpdate spam every frame.
StubMethodEmpty(module, "AudioMixerController", "Update");
StubMethodEmpty(module, "AudioMixerController", "UpdateSFXVolume");
StubMethodEmpty(module, "AudioMixerController", "Start");
StubMethodEmpty(module, "TimeController", "FixedUpdate");
StubMethodEmpty(module, "TimeController", "Update");
StubMethodEmpty(module, "TimeController", "Awake");
StubMethodEmpty(module, "TimeController", "InitializeValues");

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

static void PatchCameraControllerStart(ModuleDefinition module)
{
    var type = module.GetType("CameraController");
    var method = type?.Methods.FirstOrDefault(m => m.Name == "Start" && m.HasBody && !m.HasParameters);
    if (method == null)
    {
        Console.WriteLine("WARN: CameraController.Start not found");
        return;
    }

    if (method.Body.Instructions.Count <= 8)
    {
        Console.WriteLine("CameraController.Start already patched; skip");
        return;
    }

    var coreRef = module.AssemblyReferences.FirstOrDefault(r => r.Name == "UnityEngine.CoreModule");
    if (coreRef == null)
    {
        return;
    }

    var cameraType = new TypeReference("UnityEngine", "Camera", module, coreRef);
    var componentType = new TypeReference("UnityEngine", "Component", module, coreRef);
    var getComponent = new MethodReference("GetComponent", cameraType, componentType)
    {
        HasThis = true,
        ExplicitThis = false,
        CallingConvention = MethodCallingConvention.Generic,
    };
    getComponent.GenericParameters.Add(new GenericParameter("T", getComponent));
    var getComponentInst = new GenericInstanceMethod(getComponent);
    getComponentInst.GenericArguments.Add(cameraType);

    var camField = type.Fields.FirstOrDefault(f => f.Name == "cam");
    var defaultFov = type.Fields.FirstOrDefault(f => f.Name == "defaultFov");
    var activated = type.Fields.FirstOrDefault(f => f.Name == "activated");
    if (camField == null)
    {
        return;
    }

    var setFov = new MethodReference("set_fieldOfView", module.TypeSystem.Void, cameraType)
    {
        HasThis = true,
    };
    setFov.Parameters.Add(new ParameterDefinition(module.TypeSystem.Single));

    method.Body.Instructions.Clear();
    method.Body.Variables.Clear();
    method.Body.ExceptionHandlers.Clear();
    var il = method.Body.GetILProcessor();
    // cam = GetComponent<Camera>();
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Call, getComponentInst));
    il.Append(il.Create(OpCodes.Stfld, camField));
    // if (cam != null) { cam.fieldOfView = 90f; defaultFov = 90f; }
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldfld, camField));
    var ret = il.Create(OpCodes.Ret);
    var afterNull = il.Create(OpCodes.Nop);
    il.Append(il.Create(OpCodes.Brfalse, afterNull));
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldfld, camField));
    il.Append(il.Create(OpCodes.Ldc_R4, 90f));
    il.Append(il.Create(OpCodes.Callvirt, setFov));
    if (defaultFov != null)
    {
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Ldc_R4, 90f));
        il.Append(il.Create(OpCodes.Stfld, defaultFov));
    }

    il.Append(afterNull);
    if (activated != null)
    {
        il.Append(il.Create(OpCodes.Ldarg_0));
        il.Append(il.Create(OpCodes.Ldc_I4_1));
        il.Append(il.Create(OpCodes.Stfld, activated));
    }

    il.Append(ret);
    Console.WriteLine("Patched CameraController.Start -> safe FOV/activated only");
}

static void PatchNewMovementAwake(ModuleDefinition module)
{
    var type = module.GetType("NewMovement");
    var method = type?.Methods.FirstOrDefault(m => m.Name == "Awake" && m.HasBody);
    var gcField = type?.Fields.FirstOrDefault(f => f.Name == "gc");
    if (method == null || gcField == null)
    {
        return;
    }

    // Prepend: if (gc == null) return;
    var body = method.Body;
    var il = body.GetILProcessor();
    var first = body.Instructions[0];
    if (first.OpCode == OpCodes.Ldarg_0
        && body.Instructions.Count > 2
        && body.Instructions[1].OpCode == OpCodes.Ldfld
        && body.Instructions[1].Operand is FieldReference fr
        && fr.Name == "gc")
    {
        Console.WriteLine("NewMovement.Awake already guarded; skip");
        return;
    }

    var ret = il.Create(OpCodes.Ret);
    // Insert at end temporarily then move? InsertBefore first:
    var ldarg = il.Create(OpCodes.Ldarg_0);
    var ldfld = il.Create(OpCodes.Ldfld, gcField);
    var brtrue = il.Create(OpCodes.Brtrue, first);
    il.InsertBefore(first, ldarg);
    il.InsertBefore(first, ldfld);
    il.InsertBefore(first, brtrue);
    il.InsertBefore(first, ret);
    // Wait - order of InsertBefore reverses. Want: ldarg, ldfld, brtrue first, else ret.
    // InsertBefore(first, X) pushes X immediately before first, so last InsertBefore is first in method.
    // So we inserted: ret, brtrue, ldfld, ldarg, first — WRONG.

    // Redo cleanly:
    body.Instructions.Remove(ldarg);
    body.Instructions.Remove(ldfld);
    body.Instructions.Remove(brtrue);
    body.Instructions.Remove(ret);

    var instrs = new[]
    {
        il.Create(OpCodes.Ldarg_0),
        il.Create(OpCodes.Ldfld, gcField),
        il.Create(OpCodes.Brtrue, first),
        il.Create(OpCodes.Ret),
    };
    for (var i = instrs.Length - 1; i >= 0; i--)
    {
        il.InsertBefore(first, instrs[i]);
    }

    Console.WriteLine("Patched NewMovement.Awake -> return if gc null");
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
