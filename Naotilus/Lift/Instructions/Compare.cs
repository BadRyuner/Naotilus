namespace Naotilus.Lift.Instructions;
public class Compare(DataSource left, DataSource right) : LiftedInstruction
{
    public DataSource Left { get; } = left;
    public DataSource Right { get; } = right;
}
