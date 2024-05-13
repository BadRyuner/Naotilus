using AsmResolver;
using AsmResolver.PE.File;
using Naotilus.Enums;
using Naotilus.Platform;
using Naotilus.Structures;
using Naotilus.Utils;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using ModuleDefinition = AsmResolver.DotNet.ModuleDefinition;
using NativeReader = Internal.NativeFormat.NativeReader;

namespace Naotilus;

public sealed class NaotAssembly
{
    public readonly PEFile PeFile;

    private BasePlatform _platform;

    public ISegmentReference HydratedSegment { get; private set; }

    public byte[] HydratedData { get; private set; }

    public uint ReadyToRunHeaderRva { get; private set; }

    public ReadyToRunHeader ReadyToRunHeader { get; private set; }

    public Dictionary<uint, ModuleInfoRow> Sections { get; private set; }

    public Dictionary<uint, MethodTable> MethodTablesByRva { get; } = new(128);
    public Dictionary<int, MethodTable> MethodTablesByHashcode { get; } = new(128);

    public Dictionary<uint, string> StringTable { get; } = new(256);

    public Dictionary<ScopeDefinitionHandle, AssemblyDef> Assemblies = new(8);

    private readonly Dictionary<TypeDefinitionHandle, TypeDef> _handleToTypeDef = new(64);
    private readonly Dictionary<Handle, TypeDefBase> _handleSomeTypeToTypeDef = new(64);
    private readonly Dictionary<MethodHandle, MethodDef> _handleToMethodDef = new(64);
    private readonly Dictionary<FieldHandle, FieldDef> _handleToFieldDef = new(32);

    private byte[] _metaDataBytes;
    internal MetadataReader MetaReader;

    private NaotAssembly() { }
    public NaotAssembly(PEFile file)
    {
        PeFile = file;
        Analyze();
    }

    public static NaotAssembly FromPeFile(PEFile file) => new NaotAssembly(file);

    public ModuleDefinition Reassembly() => _platform.Reassembly();

    public IEnumerable<MethodDef> IterateAllMethods() => _handleToMethodDef.Values;

    private void Analyze()
    {
        _platform = PeFile.FileHeader.Machine switch
        {
            AsmResolver.PE.File.Headers.MachineType.Amd64 => new Amd64Platform(this),
            _ => throw new NotSupportedException(PeFile.FileHeader.Machine.ToString())
        };

        Console.WriteLine("Looking for ReadyToRun Header");

        ReadyToRunHeaderRva = _platform.GetRTRHeader();
        ReadyToRunHeader = new(PeFile.CreateReaderAtRva(ReadyToRunHeaderRva));

        Console.WriteLine("Looking for ReadyToRun Sections");

        Sections = new();
        var moduleInfos = ReadyToRunHeaderRva + ReadyToRunHeader.SizeOf;
        var reader = PeFile.CreateReaderAtRva(moduleInfos);
        for(int i = 0; i < ReadyToRunHeader.NumberOfSections; i++)
        {
            var moduleInfo = new ModuleInfoRow(PeFile, ref reader);
            Console.WriteLine($"Found {moduleInfo.SectionId} Section");
            Sections.Add((uint)moduleInfo.SectionId, moduleInfo);

            if (moduleInfo.SectionId == ReadyToRunSectionType.DehydratedData)
            {
                Console.WriteLine("Rehydrating...");
                RehydrateData(moduleInfo);
            }
        }

        ScanForStrings();
        ParseMetadata();

        Console.WriteLine("Done!");
    }

    private void RehydrateData(ModuleInfoRow dehydratedDataSection)
    {
        var start = dehydratedDataSection.Start;
        var pEnd = dehydratedDataSection.End.Rva;

        var reader = start.CreateReader();
        var at = reader.Rva;
        var offset = reader.ReadInt32();
        var hydrated = at + offset;
        HydratedSegment = PeFile.GetReferenceToRva((uint)hydrated);
        using var memStream = new MemoryStream(40960);
        using var writer = new BinaryWriter(memStream);
        
        var fixups = (long)pEnd;

        while(reader.Rva < pEnd)
        {
            DehydratedDataCommand.Decode(ref reader, out var command, out var payload);
            switch(command)
            {
                case DehydratedDataCommand.Copy:
                    if (payload == 0) Console.WriteLine("Bad copy payload.");
                    while(payload > 0)
                    {
                        writer.Write(reader.ReadByte());
                        payload--;
                    }
                    break;
                case DehydratedDataCommand.ZeroFill:
                    writer.Write(new byte[payload]);
                    break;
                case DehydratedDataCommand.PtrReloc:
                    // todo: check 32/64 bit
                    writer.Write((ulong)ReadRelPtr32((uint)(fixups + 4 * payload)));
                    break;
                case DehydratedDataCommand.RelPtr32Reloc:
                    WriteRelPtr32(ReadRelPtr32((uint)(fixups + 4 * payload)));
                    break;
                case DehydratedDataCommand.InlinePtrReloc:
                    while(payload-- > 0)
                    {
                        // todo: check 32/64 bit
                        writer.Write((ulong)ReadRelPtr32(reader.Rva));
                        reader.Rva += 4;
                    }
                    break;
                case DehydratedDataCommand.InlineRelPtr32Reloc:
                    while(payload-- > 0)
                    {
                        WriteRelPtr32(ReadRelPtr32(reader.Rva));
                        reader.Rva += 4;
                    }
                    break;
                default:
                    Console.WriteLine("bad opcode");
                    break;
            }
        }

        memStream.Position = 0;
        HydratedData = memStream.ToArray();
        //File.WriteAllBytes("D:/huh.bin", HydratedData);

        uint ReadRelPtr32(uint address)
        {
            return (uint)((int)address + PeFile.CreateReaderAtRva(address).ReadInt32());
        }

        void WriteRelPtr32(long value)
        {
            var dest = memStream.Position + hydrated;
            var val = (int)(value - dest);
            writer.Write(val);
        }
    }

    private unsafe uint TryLookForStringType()
    {
        var magic = "--- End of inner exception stack trace ---".AsSpan();
        var magicRef = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref MemoryMarshal.GetReference(magic)), magic.Length * 2);
        var stringContent = HydratedData.AsSpan().IndexOf(magicRef);
        // todo: 32 bit support
        var vtablePtrIndex = stringContent - 4 /* Length */ - 8; // vtable ptr
        var vtablePtrSpan = HydratedData.AsSpan().Slice(vtablePtrIndex, 8);
        return (uint)BinaryPrimitives.ReadInt64LittleEndian(vtablePtrSpan);
    }

    private void ScanForStrings()
    {
        Console.WriteLine("Looking for string type");
        uint stringVtablePos = 0;
        try
        {
            stringVtablePos = TryLookForStringType();
        }
        catch
        {
            Console.WriteLine("Bad image(");
        }
        if (stringVtablePos == 0) return;
        ScanForStrings(stringVtablePos);
    }

    private unsafe void ScanForStrings(uint stringVtable)
    {
        Console.WriteLine("Looking for all strings");
        Span<byte> vtableSpan = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(vtableSpan, stringVtable);

        var span = HydratedData.AsSpan();
        var rva = HydratedSegment.Rva;

        uint accumulated = 0u;
        while (true)
        {
            var next = span.IndexOf(vtableSpan);
            if (next == -1) break;
            var length = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(next + 8, 4));
            if (length != 0 && length <= 1024)
            {
                uint pos = (uint)(accumulated + next + rva);
                StringTable.Add(pos, new(new ReadOnlySpan<char>(Unsafe.AsPointer(ref MemoryMarshal.GetReference(span.Slice(next + 12))), length)));
            }
            span = span.Slice(next + 1);
            accumulated += (uint)next + 1;
        }
    }

    private unsafe void ParseMetadata()
    {
        Console.WriteLine("Parsing metadata");
        var metadata = Sections[(uint)ReadyToRunSectionType.EmbeddedMetadata];
        var len = metadata.End.Rva - metadata.Start.Rva;
        _metaDataBytes = new byte[len];
        metadata.Start.CreateReader().ReadBytes(_metaDataBytes.AsSpan());
        MetaReader = new((IntPtr)Unsafe.AsPointer(ref _metaDataBytes[0]), (int)len);
        foreach (var scopeDefinitionHandle in MetaReader.ScopeDefinitions)
        {
            var scopeDefinition = MetaReader.GetScopeDefinition(scopeDefinitionHandle);
            Assemblies.Add(scopeDefinitionHandle, new(this, scopeDefinition));
        }

        MethodTable table = default;
        uint rva = 0;
        foreach (var def in _handleToTypeDef.Values)
        {
            if (MethodTablesByHashcode.ContainsKey(def.HashCode)) continue;

            var result = FindMethodTable(def.HashCode, ref table, ref rva);
            if (result)
            {
                table.MyTypeDef = def;
                MethodTablesByRva.Add(rva, table);
                MethodTablesByHashcode.Add(def.HashCode, table);
            }
        }

        ParseInvokeMap();
    }

    private unsafe void ParseInvokeMap()
    {
        Console.WriteLine("Parsing Invoke Map");
        var externalReferencesSection = Sections[(uint)ReadyToRunSectionType.CommonFixupsTable];
        var externalReferences = externalReferencesSection.Start.Rva;

        var invokeMapSection = Sections[(uint)ReadyToRunSectionType.InvokeMap];
        var len = invokeMapSection.End.Rva - invokeMapSection.Start.Rva;
        var invokeMapBytes = new byte[len+1];
        invokeMapSection.Start.CreateReader().ReadBytes(invokeMapBytes.AsSpan());
        var reader = new NativeReader((byte*)Unsafe.AsPointer(ref invokeMapBytes[0]), len);
        var parser = new NativeParser(reader, 0);
        var hashtable = new NativeHashtable(parser);

        var lookup = hashtable.EnumerateAllEntries();
        NativeParser entryParser;
        while (!(entryParser = lookup.GetNext()).IsNull)
        {
            InvokeTableFlags entryFlags = (InvokeTableFlags)entryParser.GetUnsigned();

            bool hasMetadata = ((entryFlags & InvokeTableFlags.HasMetadataHandle) != 0);
            if (!hasMetadata)
                continue;

            var entryMethodHandle = new MethodHandle(((int)HandleType.Method << 24) | (int)entryParser.GetUnsigned());

            var index = entryParser.GetUnsigned();
            var relative = externalReferences + index * 4;
            IntPtr declaringTypeHandle = (IntPtr)(relative + ReadU32AtRVA((uint)relative));

            index = entryParser.GetUnsigned();
            relative = externalReferences + index * 4;
            IntPtr entryMethodEntrypoint = (IntPtr)(relative + ReadU32AtRVA((uint)relative));

            GetMethodDefSafe(entryMethodHandle).EntryRVA = (uint)entryMethodEntrypoint;
        }
    }

    private bool FindMethodTable(int hashcode, ref MethodTable result, ref uint rva)
    {
        Span<byte> hash = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(hash, (uint)hashcode);
        var index = HydratedData.AsSpan().IndexOf(hash);
        if (index == -1)
            return false;
        index -= 0x14;
        var start = (uint)index + HydratedSegment.Rva;
        rva = start;
        result = new(this, ref start);
        return true;
    }

    internal uint ReadPtrAtRVA(uint rva)
    {
        if (rva >= HydratedSegment.Rva && rva <= HydratedSegment.Rva + HydratedData.Length - 1)
        {
            var span = HydratedData.AsSpan().Slice((int)(rva - HydratedSegment.Rva), 8);
            return (uint)BinaryPrimitives.ReadUInt64LittleEndian(span);
        }
        return (uint)PeFile.CreateReaderAtRva(rva).ReadNativeInt(false);
    }

    internal uint ReadU32AtRVA(uint rva)
    {
        if (rva >= HydratedSegment.Rva && rva <= HydratedSegment.Rva + HydratedData.Length)
        {
            var span = HydratedData.AsSpan().Slice((int)(rva - HydratedSegment.Rva), 4);
            return BinaryPrimitives.ReadUInt32LittleEndian(span);
        }
        return PeFile.CreateReaderAtRva(rva).ReadUInt32();
    }

    internal ushort ReadU16AtRVA(uint rva)
    {
        if (rva >= HydratedSegment.Rva && rva <= HydratedSegment.Rva + HydratedData.Length)
        {
            var span = HydratedData.AsSpan().Slice((int)(rva - HydratedSegment.Rva), 2);
            return BinaryPrimitives.ReadUInt16LittleEndian(span);
        }
        return PeFile.CreateReaderAtRva(rva).ReadUInt16();
    }

    internal TypeDefBase GetTypeDefSafe(in Handle handle)
    {
        switch (handle.HandleType)
        {
            case HandleType.ArraySignature:
                return GetTypeDefSafe(handle.ToArraySignatureHandle(MetaReader));
            case HandleType.SZArraySignature:
                return GetTypeDefSafe(handle.ToSZArraySignatureHandle(MetaReader));
            case HandleType.TypeDefinition:
                return GetTypeDefSafe(handle.ToTypeDefinitionHandle(MetaReader));
            case HandleType.TypeSpecification:
                return GetTypeDefSafe(handle.ToTypeSpecificationHandle(MetaReader));
            case HandleType.ByReferenceSignature:
                return GetTypeDefSafe(handle.ToByReferenceSignatureHandle(MetaReader));
            case HandleType.TypeVariableSignature:
                return GetTypeDefSafe(handle.ToTypeVariableSignatureHandle(MetaReader));
            default:
                Console.WriteLine($"Unsupported HandleType: {handle.HandleType.ToString()}");
                return null;
        }
    }

    internal TypeDef GetTypeDefSafe(in TypeDefinitionHandle handle)
    {
        if (_handleToTypeDef.TryGetValue(handle, out var result))
            return result;
        result = new();
        _handleToTypeDef.Add(handle, result);
        result.Read(this, handle);
        return result;
    }
    
    internal TypeDefBase GetTypeDefSafe(in TypeSpecificationHandle handle)
    {
        if (_handleSomeTypeToTypeDef.TryGetValue(handle, out var result))
            return result;
        result = GetTypeDefSafe(handle.GetTypeSpecification(MetaReader).Signature);
        _handleSomeTypeToTypeDef.Add(handle, result);
        return result;
    }

    internal TypeDefBase GetTypeDefSafe(in SZArraySignatureHandle handle)
    {
        if (_handleSomeTypeToTypeDef.TryGetValue(handle, out var result))
            return result;
        var res = new SZArrayDef();
        _handleSomeTypeToTypeDef.Add(handle, res);
        res.Read(this, handle);
        return res;
    }

    internal TypeDefBase GetTypeDefSafe(in ArraySignatureHandle handle)
    {
        if (_handleSomeTypeToTypeDef.TryGetValue(handle, out var result))
            return result;
        var res = new ArrayDef();
        _handleSomeTypeToTypeDef.Add(handle, res);
        res.Read(this, handle);
        return res;
    }

    internal TypeDefBase GetTypeDefSafe(in ByReferenceSignatureHandle handle)
    {
        if (_handleSomeTypeToTypeDef.TryGetValue(handle, out var result))
            return result;
        var res = new ByRefTypeDef();
        _handleSomeTypeToTypeDef.Add(handle, res);
        res.Read(this, handle);
        return res;
    }

    internal TypeDefBase GetTypeDefSafe(in TypeVariableSignatureHandle handle)
    {
        if (_handleSomeTypeToTypeDef.TryGetValue(handle, out var result))
            return result;
        var res = new TypeVarSigDef();
        _handleSomeTypeToTypeDef.Add(handle, res);
        res.Read(this, handle);
        return res;
    }

    internal MethodDef GetMethodDefSafe(in MethodHandle handle)
    {
        if (_handleToMethodDef.TryGetValue(handle, out var result))
            return result;
        result = new();
        _handleToMethodDef.Add(handle, result);
        result.Read(this, handle);
        return result;
    }

    internal FieldDef GetFieldDefSafe(in FieldHandle handle)
    {
        if (_handleToFieldDef.TryGetValue(handle, out var result))
            return result;
        result = new();
        _handleToFieldDef.Add(handle, result);
        result.Read(this, handle);
        return result;
    }
}
