using AsmResolver.IO;
using System.Runtime.CompilerServices;

namespace Naotilus.Structures;
public sealed class ReadyToRunHeader
{
    public readonly static uint SizeOf = 0x10;

    public readonly uint Signature;
    public readonly ushort MajorVersion;
    public readonly ushort MinorVersion;
    public readonly uint Flags;
    public readonly ushort NumberOfSections;
    public readonly byte EntrySize;
    public readonly byte EntryType;

    public ReadyToRunHeader(BinaryStreamReader reader)
    {
        Signature = reader.ReadUInt32();
        MajorVersion = reader.ReadUInt16();
        MinorVersion = reader.ReadUInt16();
        Flags = reader.ReadUInt32();
        NumberOfSections = reader.ReadUInt16();
        EntrySize = reader.ReadByte();
        EntryType = reader.ReadByte();
    }
}
