using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.DotNet.Signatures.Types;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
using Naotilus.Lift;

namespace Naotilus.Platform;
internal abstract class BasePlatform
{
    internal BasePlatform(NaotAssembly ass) => Assembly = ass;

    internal uint EntryPoint { get; set; }

    internal readonly NaotAssembly Assembly;

    internal abstract uint GetRTRHeader();

    internal abstract LiftedFunction LiftFunction(uint at);

    internal ModuleDefinition Reassembly()
    {
        var module = new ModuleDefinition("NAOT", KnownCorLibs.SystemPrivateCoreLib_v8_0_0_0);
        PVoid = module.CorLibTypeFactory.Void.MakePointerType();
        PVoid = module.DefaultImporter.ImportTypeSignature(PVoid);
        Container = module.GetOrCreateModuleType();

        ExceptionCtor = module.DefaultImporter.ImportMethod(typeof(Exception).GetConstructor([typeof(string)]));

        BaseMethodSignature = MethodSignature.CreateStatic(module.CorLibTypeFactory.Void);

        var entry = new MethodDefinition("EntryPoint", MethodAttributes.Static | MethodAttributes.Public, BaseMethodSignature);
        var body = new CilMethodBody(entry);
        entry.CilMethodBody = body;
        LiftFunction(EntryPoint).CompileTo(entry);
        ReassembliedFunctions.Add(EntryPoint, entry);
        Container.Methods.Add(entry);

        for (int i = 0; i < PlaceHolders.Count; i++)
        {
            var key = PlaceHolders.Keys.ElementAt(i);
            LiftFunction(key).CompileTo(PlaceHolders[key]);
        }

        return module;
    }

    protected void Unsupported<T>(CilInstructionCollection cilInstructions, ref T thing)
    {
        cilInstructions.Add(CilOpCodes.Ldstr, $"Unsupported: {thing}");
        cilInstructions.Add(CilOpCodes.Newobj, ExceptionCtor);
        cilInstructions.Add(CilOpCodes.Throw);
    }

    protected MethodDefinition GetMethodAt(uint rva)
    {
        if (ReassembliedFunctions.TryGetValue(rva, out var result))
            return result;
        if (PlaceHolders.TryGetValue(rva, out result))
            return result;
        result = new MethodDefinition(rva.ToString("method:0x{X2}"), MethodAttributes.Static | MethodAttributes.Public, BaseMethodSignature);
        var b = new CilMethodBody(result);
        result.CilMethodBody = b;
        PlaceHolders.Add(rva, result);
        Container.Methods.Add(result);
        return result;
    }

    protected TypeDefinition Container;

    private Dictionary<uint, MethodDefinition> ReassembliedFunctions = new();
    private Dictionary<uint, MethodDefinition> PlaceHolders = new();

    protected MethodSignature BaseMethodSignature;

    protected TypeSignature PVoid;

    protected IMethodDescriptor ExceptionCtor;
}
