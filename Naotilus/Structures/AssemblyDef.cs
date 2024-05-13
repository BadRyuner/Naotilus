using System.Diagnostics;
using Internal.Metadata.NativeFormat;

namespace Naotilus.Structures;
[DebuggerDisplay("Assembly: {Name}")]
public sealed class AssemblyDef
{
    public readonly AssemblyFlags Flags;
    public readonly string Name;
    public readonly AssemblyHashAlgorithm HashAlgorithm;
    public readonly ushort MajorVersion;
    public readonly ushort MinorVersion;
    public readonly ushort BuildNumber;
    public readonly ushort RevisionNumber;
    public readonly byte[] PublicKey;
    public readonly string Culture;
    public readonly TypeDef[] Types;
    public readonly MethodDef EntryPoint;
    public readonly TypeDef GlobalModuleType;
    public readonly CustomAttributeDef[] CustomAttributes;
    public readonly string ModuleName;
    public readonly byte[] Mvid;
    public readonly CustomAttributeDef[] ModuleAttributes;

    public readonly int HashCode;

    public AssemblyDef(NaotAssembly ass, in ScopeDefinition scopeDefinition)
    {
        var reader = ass.MetaReader;

        HashCode = scopeDefinition.Handle.GetHashCode();

        Flags = scopeDefinition.Flags;
        Name = reader.GetConstantStringValue(scopeDefinition.Name).Value;
        HashAlgorithm = scopeDefinition.HashAlgorithm;
        MajorVersion = scopeDefinition.MajorVersion;
        MinorVersion = scopeDefinition.MinorVersion;
        RevisionNumber = scopeDefinition.RevisionNumber;
        BuildNumber = scopeDefinition.BuildNumber;

        PublicKey = new byte[scopeDefinition.PublicKey.Count];
        int counter = 0;
        foreach (var b in scopeDefinition.PublicKey)
        {
            PublicKey[counter] = b;
            counter++;
        }

        Culture = reader.GetConstantStringValue(scopeDefinition.Culture).Value;

        var rootNamespaceDef = reader.GetNamespaceDefinition(scopeDefinition.RootNamespaceDefinition);
        Types = new TypeDef[rootNamespaceDef.TypeDefinitions.Count];
        counter = 0;
        foreach (var type in rootNamespaceDef.TypeDefinitions)
        {
            Types[counter] = ass.GetTypeDefSafe(type); 
            counter++;
        }

        if (!scopeDefinition.EntryPoint.IsNil)
            EntryPoint = ass.GetMethodDefSafe(scopeDefinition.EntryPoint.GetQualifiedMethod(ass.MetaReader).Method);

        if (!scopeDefinition.GlobalModuleType.IsNil)
            GlobalModuleType = ass.GetTypeDefSafe(scopeDefinition.GlobalModuleType);

        CustomAttributes = new CustomAttributeDef[scopeDefinition.CustomAttributes.Count];
        counter = 0;
        foreach (var attrHandle in scopeDefinition.CustomAttributes)
        {
            CustomAttributes[counter] = new(ass, attrHandle);
            counter++;
        }

        ModuleName = reader.GetConstantStringValue(scopeDefinition.ModuleName).Value;

        Mvid = new byte[scopeDefinition.Mvid.Count];
        counter = 0;
        foreach (var b in scopeDefinition.Mvid)
        {
            Mvid[counter] = b;
            counter++;
        }

        ModuleAttributes = new CustomAttributeDef[scopeDefinition.ModuleCustomAttributes.Count];
        counter = 0;
        foreach (var attrHandle in scopeDefinition.ModuleCustomAttributes)
        {
            ModuleAttributes[counter] = new(ass, attrHandle);
            counter++;
        }
    }
}