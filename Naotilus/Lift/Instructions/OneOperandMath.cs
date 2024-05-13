namespace Naotilus.Lift.Instructions;
internal class OneOperandMath(DataSource left, DataSource right, OneOperandMath.MathType type) : LiftedInstruction
{
    public DataSource Left => left;
    public DataSource Right => right;
    public MathType Type => type;

    public enum MathType : byte
    {
        Neg,
        Not
    }
}
