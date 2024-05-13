using AsmResolver.IO;
using System.Runtime.CompilerServices;

namespace Naotilus.Utils;

internal static class DehydratedDataCommand
{
    public const byte Copy = 0x00;
    public const byte ZeroFill = 0x01;
    public const byte RelPtr32Reloc = 0x02;
    public const byte PtrReloc = 0x03;
    public const byte InlineRelPtr32Reloc = 0x04;
    public const byte InlinePtrReloc = 0x05;

    private const byte DehydratedDataCommandMask = 0x07;
    private const int DehydratedDataCommandPayloadShift = 3;

    private const int MaxRawShortPayload = (1 << (8 - DehydratedDataCommandPayloadShift)) - 1;
    private const int MaxExtraPayloadBytes = 3;
    public const int MaxShortPayload = MaxRawShortPayload - MaxExtraPayloadBytes;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Decode(ref BinaryStreamReader reader, out int command, out int payload)
    {
        byte b = reader.ReadByte();
        command = b & DehydratedDataCommandMask;
        payload = b >> DehydratedDataCommandPayloadShift;
        int extraBytes = payload - MaxShortPayload;
        if (extraBytes > 0)
        {
            payload = reader.ReadByte();
            if (extraBytes > 1)
            {
                payload += reader.ReadByte() << 8;
                if (extraBytes > 2)
                    payload += reader.ReadByte() << 16;
            }

            payload += MaxShortPayload;
        }
    }
}
