// Patches retail ULTRAKILL managed assemblies so they can be AOT-compiled with IL2CPP for iOS.
//
// Usage: UkPatcher <retailManagedDir> <outDir> <assembly list file>
//
//  * every P/Invoke (Steam, Discord, MOC, SameFrameReadback, ...) becomes a managed stub that throws
//    DllNotFoundException, so IL2CPP never emits references to native libraries we don't have on iOS.
//  * Application.dataPath (used by the game to locate Saves/Preferences/CyberGrind next to the exe)
//    is redirected to <Documents>/ULTRAKILL_Data so those folders land in the app's Documents.
//  * a few helpers that poke at native Unity object memory are replaced with managed equivalents.
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("usage: UkPatcher <managedDir> <outDir> <assemblyListFile>");
            return 1;
        }
        string inDir = args[0], outDir = args[1];
        var names = File.ReadAllLines(args[2]).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToList();
        Directory.CreateDirectory(outDir);

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(inDir);
        var rp = new ReaderParameters { AssemblyResolver = resolver, ReadingMode = ReadingMode.Immediate, ReadSymbols = false };

        int total = 0;
        foreach (var name in names)
        {
            string path = Path.Combine(inDir, name);
            var asm = AssemblyDefinition.ReadAssembly(path, rp);
            int n = StubPInvokes(asm);
            if (asm.Name.Name == "Assembly-CSharp")
                n += PatchGame(asm);
            total += n;
            asm.Write(Path.Combine(outDir, name));
            Console.WriteLine($"{name}: {n} patches");
        }
        Console.WriteLine($"total {total}");
        return 0;
    }

    static IEnumerable<TypeDefinition> AllTypes(ModuleDefinition m) => m.GetTypes();

    static int StubPInvokes(AssemblyDefinition asm)
    {
        int n = 0;
        var mod = asm.MainModule;
        var dnfType = new TypeReference("System", "DllNotFoundException", mod, mod.TypeSystem.CoreLibrary);
        var dllNotFoundCtor = new MethodReference(".ctor", mod.TypeSystem.Void, dnfType) { HasThis = true };
        dllNotFoundCtor.Parameters.Add(new ParameterDefinition(mod.TypeSystem.String));
        foreach (var t in AllTypes(mod))
        {
            foreach (var m in t.Methods)
            {
                if (!m.IsPInvokeImpl || m.PInvokeInfo == null)
                    continue;
                string lib = m.PInvokeInfo.Module.Name;
                m.PInvokeInfo = null;     // Cecil's setter turns IsPInvokeImpl back on,
                m.IsPInvokeImpl = false;  // so clear the flag afterwards
                m.IsPreserveSig = false;
                m.ImplAttributes &= ~MethodImplAttributes.PreserveSig;
                m.ImplAttributes |= MethodImplAttributes.IL | MethodImplAttributes.Managed;
                m.Body = new MethodBody(m);
                var il = m.Body.GetILProcessor();
                il.Emit(OpCodes.Ldstr, lib + " is not available on iOS");
                il.Emit(OpCodes.Newobj, dllNotFoundCtor);
                il.Emit(OpCodes.Throw);
                n++;
            }
        }
        return n;
    }

    static int PatchGame(AssemblyDefinition asm)
    {
        var mod = asm.MainModule;
        int n = 0;

        // --- IosPaths helper -------------------------------------------------------------
        var coreModule = mod.AssemblyReferences.First(r => r.Name == "UnityEngine.CoreModule");
        var appType = new TypeReference("UnityEngine", "Application", mod, coreModule);
        var getPersistent = new MethodReference("get_persistentDataPath", mod.TypeSystem.String, appType);
        var concat = new MethodReference("Concat", mod.TypeSystem.String, mod.TypeSystem.String);
        concat.Parameters.Add(new ParameterDefinition(mod.TypeSystem.String));
        concat.Parameters.Add(new ParameterDefinition(mod.TypeSystem.String));

        var helper = new TypeDefinition("UltrakillIOS", "IosPaths",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            mod.TypeSystem.Object);
        mod.Types.Add(helper);
        var dataPath = new MethodDefinition("get_dataPath",
            MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, mod.TypeSystem.String);
        helper.Methods.Add(dataPath);
        {
            var il = dataPath.Body.GetILProcessor();
            il.Emit(OpCodes.Call, getPersistent);
            il.Emit(OpCodes.Ldstr, "/ULTRAKILL_Data");
            il.Emit(OpCodes.Call, concat);
            il.Emit(OpCodes.Ret);
        }

        foreach (var t in AllTypes(mod))
        {
            if (t == helper) continue;
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (var ins in m.Body.Instructions)
                {
                    if ((ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) && ins.Operand is MethodReference mr &&
                        mr.Name == "get_dataPath" && mr.DeclaringType.FullName == "UnityEngine.Application")
                    {
                        ins.OpCode = OpCodes.Call;
                        ins.Operand = dataPath;
                        n++;
                    }
                }
            }
        }

        // --- report no compute support: the game then uses its non-compute fallbacks (the two compute
        //     shaders it ships only have DX11/GL/Vulkan kernels) -------------------------------------
        foreach (var t in AllTypes(mod))
        {
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (var ins in m.Body.Instructions)
                {
                    if (ins.OpCode == OpCodes.Call && ins.Operand is MethodReference mr &&
                        mr.Name == "get_supportsComputeShaders" && mr.DeclaringType.FullName == "UnityEngine.SystemInfo")
                    {
                        ins.OpCode = OpCodes.Ldc_I4_0;
                        ins.Operand = null;
                        n++;
                    }
                }
            }
        }

        // --- AudioSourceExtensions.IsPaused: read native m_pause -> managed approximation -------
        var ase = mod.GetType("AudioSourceExtensions");
        var isPaused = ase?.Methods.FirstOrDefault(m => m.Name == "IsPaused");
        if (isPaused != null)
        {
            var audioModule = mod.AssemblyReferences.First(r => r.Name == "UnityEngine.AudioModule");
            var srcType = new TypeReference("UnityEngine", "AudioSource", mod, audioModule);
            var isPlaying = new MethodReference("get_isPlaying", mod.TypeSystem.Boolean, srcType) { HasThis = true };
            var getTime = new MethodReference("get_time", mod.TypeSystem.Single, srcType) { HasThis = true };
            isPaused.Body = new MethodBody(isPaused);
            var il = isPaused.Body.GetILProcessor();
            var retFalse = il.Create(OpCodes.Ldc_I4_0);
            // return !src.isPlaying && src.time > 0f;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, isPlaying);
            il.Emit(OpCodes.Brtrue_S, retFalse);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, getTime);
            il.Emit(OpCodes.Ldc_R4, 0f);
            il.Emit(OpCodes.Cgt);
            il.Emit(OpCodes.Ret);
            il.Append(retFalse);
            il.Emit(OpCodes.Ret);
            n++;
        }

        // --- GameObjectExtensions.SwapComponents: rewrites native component vector -> no-op ---
        var goe = mod.GetType("GameObjectExtensions");
        var swap = goe?.Methods.FirstOrDefault(m => m.Name == "SwapComponents");
        if (swap != null)
        {
            swap.Body = new MethodBody(swap);
            swap.Body.GetILProcessor().Emit(OpCodes.Ret);
            n++;
        }

        n += PatchCursor(mod, coreModule);

        // --- IntroText.waitingForInput public, so the touch HUD can show YES / NO buttons ---
        var introText = mod.GetType("IntroText");
        var waiting = introText?.Fields.FirstOrDefault(f => f.Name == "waitingForInput");
        if (waiting != null)
        {
            waiting.Attributes = (waiting.Attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
            n++;
        }
        return n;
    }

    // Cursor.lockState / Cursor.visible don't mean anything on iOS (and may not read back what was set),
    // but they are how the game says "a cursor UI is up". Route the game's setters through
    // UltrakillIOS.IosCursor, which remembers the values for the touch HUD and then calls Unity.
    static int PatchCursor(ModuleDefinition mod, AssemblyNameReference coreModule)
    {
        int n = 0;
        var cursorType = new TypeReference("UnityEngine", "Cursor", mod, coreModule);
        var lockModeType = new TypeReference("UnityEngine", "CursorLockMode", mod, coreModule) { IsValueType = true };
        var setLock = new MethodReference("set_lockState", mod.TypeSystem.Void, cursorType);
        setLock.Parameters.Add(new ParameterDefinition(lockModeType));
        var setVisible = new MethodReference("set_visible", mod.TypeSystem.Void, cursorType);
        setVisible.Parameters.Add(new ParameterDefinition(mod.TypeSystem.Boolean));

        var shim = new TypeDefinition("UltrakillIOS", "IosCursor",
            TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            mod.TypeSystem.Object);
        mod.Types.Add(shim);
        var fs = FieldAttributes.Public | FieldAttributes.Static;
        var fLock = new FieldDefinition("LockState", fs, mod.TypeSystem.Int32);
        var fVisible = new FieldDefinition("Visible", fs, mod.TypeSystem.Boolean);
        var fFrame = new FieldDefinition("Version", fs, mod.TypeSystem.Int32);   // bumps on every set
        shim.Fields.Add(fLock);
        shim.Fields.Add(fVisible);
        shim.Fields.Add(fFrame);

        MethodDefinition Make(string name, TypeReference param, FieldDefinition field, MethodReference target)
        {
            var m = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig, mod.TypeSystem.Void);
            m.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, param));
            shim.Methods.Add(m);
            var il = m.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ldsfld, fFrame);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stsfld, fFrame);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
            return m;
        }
        var shimLock = Make("SetLockState", lockModeType, fLock, setLock);
        var shimVisible = Make("SetVisible", mod.TypeSystem.Boolean, fVisible, setVisible);

        foreach (var t in AllTypes(mod))
        {
            if (t == shim) continue;
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (var ins in m.Body.Instructions)
                {
                    if (ins.OpCode != OpCodes.Call || !(ins.Operand is MethodReference mr) ||
                        mr.DeclaringType.FullName != "UnityEngine.Cursor") continue;
                    if (mr.Name == "set_lockState") { ins.Operand = shimLock; n++; }
                    else if (mr.Name == "set_visible") { ins.Operand = shimVisible; n++; }
                }
            }
        }
        return n;
    }
}
