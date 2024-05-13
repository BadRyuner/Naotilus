using AsmResolver.IO;
using AsmResolver.PE.File;

namespace Naotilus;
internal static class Extensions
{
    internal static uint FixRVA(this PEFile file, uint rva) => rva - (uint)file.OptionalHeader.ImageBase;

    internal static uint ReadRVA(this ref BinaryStreamReader reader, PEFile file)
    {
        uint rva;
        if (file.FileHeader.Machine is AsmResolver.PE.File.Headers.MachineType.Amd64)
            rva = (uint)reader.ReadUInt64();
        else
            rva = reader.ReadUInt32();
        if (rva == 0)
            return rva;
        return rva - (uint)file.OptionalHeader.ImageBase;
    }
}
