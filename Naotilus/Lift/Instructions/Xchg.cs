namespace Naotilus.Lift.Instructions;
public sealed class Xchg(DataSource left, DataSource right) : LiftedInstruction
{
    public DataSource Left { get; } = left;
    public DataSource Right { get; } = right;
}
