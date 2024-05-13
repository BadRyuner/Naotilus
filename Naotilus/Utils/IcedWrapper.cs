using AsmResolver.IO;
using Iced.Intel;

namespace Naotilus.Utils;
internal class IcedWrapper : CodeReader
{
    internal BinaryStreamReader Reader;

    internal IcedWrapper(in BinaryStreamReader reader)
    {
        Reader = reader;
    }

    internal void Offset(int offset) => Reader.Offset = (ulong)(((long)Reader.Offset) + offset);

    public override int ReadByte() => Reader.ReadByte();
}
