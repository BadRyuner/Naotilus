using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Naotilus.Lift.Instructions;
public sealed class Branch(uint dest, Branch.BrType type) : LiftedInstruction
{
    public uint Dest { get; } = dest;
    public BrType Type { get; } = type;

    public enum BrType
    {
        LO, NO, NE, EQ, GT, GE, LT, LE
    }
}
