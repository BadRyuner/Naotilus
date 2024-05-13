using System.Reflection;
using System.Reflection.Metadata;
using Internal.Metadata.NativeFormat;

namespace Naotilus.Structures;
public sealed class FieldDef
{
    public string Name;
    public TypeDefBase Type;
    public FieldAttributes Flags;
    public uint Offset;

    internal void Read(NaotAssembly ass, in FieldHandle handle)
    {
        var def = handle.GetField(ass.MetaReader);
        Name = def.Name.GetConstantStringValue(ass.MetaReader).Value;
        Flags = def.Flags;
        Offset = def.Offset;
        Type = ass.GetTypeDefSafe(def.Signature.GetFieldSignature(ass.MetaReader).Type);
    }
}
