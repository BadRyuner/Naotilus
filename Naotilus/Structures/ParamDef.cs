using System.Reflection;
using Internal.Metadata.NativeFormat;

namespace Naotilus.Structures;
public sealed class ParamDef
{
    public readonly string Name;
    public readonly ParameterAttributes Flags;
    public readonly CustomAttributeDef[] CustomAttributes;
    public readonly ushort Sequence;

    public ParamDef(NaotAssembly ass, in ParameterHandle handle)
    {
        var def = handle.GetParameter(ass.MetaReader);

        Name = def.Name.GetConstantStringValue(ass.MetaReader).Value;
        Flags = def.Flags;

        CustomAttributes = new CustomAttributeDef[def.CustomAttributes.Count];
        var counter = 0;
        foreach (var attrHandle in def.CustomAttributes)
        {
            CustomAttributes[counter] = new(ass, attrHandle);
            counter++;
        }

        Sequence = def.Sequence;
    }
}
