namespace Naotilus.Lift.Instructions;
public sealed class Math(DataSource left, DataSource right, Math.MathType type) : LiftedInstruction
{
    public DataSource Left => left;
    public DataSource Right => right;
    public MathType Type => type;

    public enum MathType : byte
    {
        Add,
        Sub,
        Mul,
        Div,
        Or,
        Xor,
        And,
        ShiftLeft,
        ShiftRight,
    }
}
