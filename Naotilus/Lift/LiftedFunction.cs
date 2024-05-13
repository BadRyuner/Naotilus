using AsmResolver.DotNet;

namespace Naotilus.Lift;
public sealed class LiftedFunction
{
    public uint RVA;

    public Dictionary<uint, LiftedInstruction> Instructions = new(2);

    public void CompileTo(MethodDefinition def)
    {

    }
}
