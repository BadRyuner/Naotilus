using Internal.Metadata.NativeFormat;

namespace Naotilus.Structures;
public sealed class CustomAttributeDef
{
    public readonly MethodDef Constructor;

    public CustomAttributeDef(NaotAssembly ass, in CustomAttributeHandle handle)
    {
        var def = handle.GetCustomAttribute(ass.MetaReader);

        if (def.Constructor.HandleType == HandleType.QualifiedMethod)
            Constructor = ass.GetMethodDefSafe(def.Constructor
                .ToQualifiedMethodHandle(ass.MetaReader)
                .GetQualifiedMethod(ass.MetaReader).Method);

        // TODO: args
    }
}
