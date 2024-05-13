using Naotilus.Structures;

namespace Naotilus.Lift.Instructions;
public sealed class Call : LiftedInstruction
{
    public uint RVA;
    public MethodDef ResolvedDef;
}
