using AsmResolver.IO;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Naotilus.Structures;
public struct MethodTable
{
    public readonly uint MyRVA;

    public TypeDef MyTypeDef;

    public readonly FlagsSizeUnion Flags;
    public readonly uint BaseSize;
    public readonly nint RelatedTypeUnion;
    public readonly ushort NumVtableSlots;
    public readonly ushort NumInterfaces;
    public readonly uint HashCode;
    public readonly nint[] Vtable;
    public readonly nint[] Interfaces;

    public MethodTable(NaotAssembly ass, ref uint at)
    {
        MyRVA = at;
        Flags = new(ass.ReadU32AtRVA(at)); at += 4;
        BaseSize = ass.ReadU32AtRVA(at); at += 4;
        RelatedTypeUnion = (nint)ass.ReadPtrAtRVA(at); at += 8;
        NumVtableSlots = ass.ReadU16AtRVA(at); at += 2;
        NumInterfaces = ass.ReadU16AtRVA(at); at += 2;
        HashCode = ass.ReadU32AtRVA(at); at += 4;
        if (ElementType == EETypeElementType.ElementType_Class && NumInterfaces < 256)
        {
            Vtable = new nint[NumVtableSlots];
            for (int i = 0; i < NumVtableSlots; i++)
            {
                Vtable[i] = (nint)ass.ReadPtrAtRVA(at); at += 8;
            }
            Interfaces = new nint[NumInterfaces];
            for (int i = 0; i < NumInterfaces; i++)
            {
                Interfaces[i] = (nint)ass.ReadPtrAtRVA(at); at += 8;
            }
        }
        else
            Vtable = Array.Empty<nint>();
    }

    public EETypeElementType ElementType => (EETypeElementType)(((uint)Flags.Flags & (uint)EFlags.ElementTypeMask) >> (int)EFlags.ElementTypeShift);

    public bool IsArray => ElementType == EETypeElementType.ElementType_Array || IsSzArray;

    public bool IsSzArray => ElementType == EETypeElementType.ElementType_SzArray;

    public Kinds Kind => (Kinds)(((uint)Flags.Flags & (uint)EFlags.EETypeKindMask) >> (int)EFlags.EETypeKindMask);

    public bool IsParameterizedType => Kind == Kinds.ParameterizedEEType;

    public bool IsInterface => ElementType == EETypeElementType.ElementType_Interface;

    public bool IsValueType => (uint)ElementType < (uint)EETypeElementType.ElementType_Class;

    public bool HasFinalizer => ((uint)Flags.Flags & (uint)EFlags.HasFinalizerFlag) != 0 && !HasComponentSize;

    public bool HasEagerFinalizer => ((uint)Flags.Flags & (uint)EFlags.HasEagerFinalizerFlag) != 0 && !HasComponentSize;

    public bool HasCriticalFinalizer => ((uint)Flags.Flags & (uint)EFlags.HasCriticalFinalizerFlag) != 0 && !HasComponentSize;

    public bool IsTrackedReferenceWithFinalizer => ((uint)Flags.Flags & (uint)EFlags.IsTrackedReferenceWithFinalizerFlag) != 0 && !HasComponentSize;

    public bool HasComponentSize => (int)Flags.Flags < 0;

    public bool HasReferenceFields => ((uint)Flags.Flags & (uint)EFlags.HasPointersFlag) != 0;

    public bool HasGenericVariance => Flags.Flags.HasFlag(EFlags.GenericVarianceFlag);

    public override bool Equals([NotNullWhen(true)] object obj) => obj is MethodTable mt && mt.HashCode == this.HashCode;

    public override int GetHashCode() => (int)HashCode;

    [StructLayout(LayoutKind.Explicit)]
    public struct FlagsSizeUnion
    {
        public FlagsSizeUnion(uint data) => Flags = (EFlags)data;

        [FieldOffset(0)] public EFlags Flags;
        [FieldOffset(0)] public ushort ComponentSize;
    }

    public enum EFlags : uint
    {
        EETypeKindMask = 0x00030000,
        OptionalFieldsFlag = 0x00040000,
        CollectibleFlag = 0x00200000,
        IsDynamicTypeFlag = 0x00080000,
        HasFinalizerFlag = 0x00100000,
        HasPointersFlag = 0x01000000,
        GenericVarianceFlag = 0x00800000,
        IsGenericFlag = 0x02000000,
        ElementTypeMask = 0x7C000000,
        ElementTypeShift = 26,
        HasComponentSizeFlag = 0x80000000,

        HasEagerFinalizerFlag = 0x0001,
        HasCriticalFinalizerFlag = 0x0002,
        IsTrackedReferenceWithFinalizerFlag = 0x0004,
    };

    public enum EETypeElementType : byte
    {
        ElementType_Unknown = 0x00,
        ElementType_Void = 0x01,
        ElementType_Boolean = 0x02,
        ElementType_Char = 0x03,
        ElementType_SByte = 0x04,
        ElementType_Byte = 0x05,
        ElementType_Int16 = 0x06,
        ElementType_UInt16 = 0x07,
        ElementType_Int32 = 0x08,
        ElementType_UInt32 = 0x09,
        ElementType_Int64 = 0x0A,
        ElementType_UInt64 = 0x0B,
        ElementType_IntPtr = 0x0C,
        ElementType_UIntPtr = 0x0D,
        ElementType_Single = 0x0E,
        ElementType_Double = 0x0F,

        ElementType_ValueType = 0x10,
        ElementType_Nullable = 0x12,

        ElementType_Class = 0x14,
        ElementType_Interface = 0x15,

        ElementType_SystemArray = 0x16,

        ElementType_Array = 0x17,
        ElementType_SzArray = 0x18,
        ElementType_ByRef = 0x19,
        ElementType_Pointer = 0x1A,
        ElementType_FunctionPointer = 0x1B,
    };

    public enum Kinds
    {
        CanonicalEEType = 0x00000000,
        ParameterizedEEType = 0x00020000,
        GenericTypeDefEEType = 0x00030000,
    };
}
