using Naotilus.Structures;

namespace Naotilus.Lift.Instructions;
internal class Jump : LiftedInstruction
{
    public uint RVA;
    public MethodDef ResolvedDef;
}
