namespace Naotilus.Lift.Instructions;
public sealed class Test(DataSource left, DataSource right) : LiftedInstruction
{
    public DataSource Left { get; } = left;
    public DataSource Right { get; } = right;
}
