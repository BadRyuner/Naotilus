namespace Naotilus.Enums;
[Flags]
public enum InvokeTableFlags : uint
{
    HasVirtualInvoke = 0x00000001,
    IsGenericMethod = 0x00000002,
    HasMetadataHandle = 0x00000004,
    IsDefaultConstructor = 0x00000008,
    RequiresInstArg = 0x00000010,
    HasEntrypoint = 0x00000020,
    IsUniversalCanonicalEntry = 0x00000040,
    NeedsParameterInterpretation = 0x00000080,
    CallingConventionDefault = 0x00000000,
    Cdecl = 0x00001000,
    Winapi = 0x00002000,
    StdCall = 0x00003000,
    ThisCall = 0x00004000,
    FastCall = 0x00005000,
    CallingConventionMask = 0x00007000,
}