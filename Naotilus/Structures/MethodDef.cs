using Internal.Metadata.NativeFormat;
using System.Reflection;

namespace Naotilus.Structures;
public sealed class MethodDef
{
    public string Name;
    public MethodAttributes Flags;
    public MethodImplAttributes ImplFlags;
    public CustomAttributeDef[] CustomAttributes;
    public ParamDef[] ParametersDef;
    public TypeDefBase[] Parameters;
    public TypeDefBase ReturnType;

    public uint EntryRVA;

    public int HashCode;

    internal void Read(NaotAssembly ass, in MethodHandle handle)
    {
        HashCode = handle.GetHashCode();

        var def = handle.GetMethod(ass.MetaReader);
        Name = def.Name.GetConstantStringValue(ass.MetaReader).Value;
        Flags = def.Flags;

        CustomAttributes = new CustomAttributeDef[def.CustomAttributes.Count];
        var counter = 0;
        foreach (var attrHandle in def.CustomAttributes)
        {
            CustomAttributes[counter] = new(ass, attrHandle);
            counter++;
        }

        ImplFlags = def.ImplFlags;

        ParametersDef = new ParamDef[def.Parameters.Count];
        counter = 0;
        foreach (var h in def.Parameters)
        {
            ParametersDef[counter] = new(ass, h);
            counter++;
        }

        var sig = def.Signature.GetMethodSignature(ass.MetaReader);

        Parameters = new TypeDefBase[sig.Parameters.Count];
        counter = 0;
        foreach (var h in sig.Parameters)
        {
            Parameters[counter] = ass.GetTypeDefSafe(h);
            counter++;
        }

        ReturnType = ass.GetTypeDefSafe(sig.ReturnType);
    }
}
