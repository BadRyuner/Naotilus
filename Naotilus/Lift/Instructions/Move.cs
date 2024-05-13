namespace Naotilus.Lift.Instructions;
public sealed class Move(DataSource dest, DataSource from) : LiftedInstruction
{
    public DataSource Dest { get; } = dest;
    public DataSource From { get; } = from;
}
