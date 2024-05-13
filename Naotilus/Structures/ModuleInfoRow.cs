using AsmResolver;
using AsmResolver.IO;
using AsmResolver.PE.File;
using Naotilus.Enums;
using System.Diagnostics;

namespace Naotilus.Structures;

[DebuggerDisplay("{SectionId} {Flags}")]
public sealed class ModuleInfoRow
{
    public readonly ReadyToRunSectionType SectionId;
    public readonly uint Flags;
    public readonly ISegmentReference Start;
    public readonly ISegmentReference End;

    public bool HasEndPointer => Flags == 0x1;

    public ModuleInfoRow(PEFile file, ref BinaryStreamReader reader)
    {
        SectionId = (ReadyToRunSectionType)reader.ReadUInt32();
        Flags = reader.ReadUInt32();
        Start = file.GetReferenceToRva(reader.ReadRVA(file));
        End = file.GetReferenceToRva(reader.ReadRVA(file));
    }
}
