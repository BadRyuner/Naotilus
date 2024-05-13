using System.Runtime.CompilerServices;
using Naotilus.Structures;

namespace Naotilus.Lift;
public abstract class DataSource
{

}

public sealed class ConstantIntDataSource(int data) : DataSource
{
    public int Data => data;
}

public sealed class ConstantLongDataSource(long data) : DataSource
{
    public long Data => data;
}

public sealed class ConstantFloatDataSource(float data) : DataSource
{
    public float Data => data;
}

public sealed class ConstantDoubleDataSource(double data) : DataSource
{
    public double Data => data;
}

public sealed class ConstantStringDataSource(string data) : DataSource
{
    public string Data => data;
}

public sealed class ConstantVtableDataSource(MethodTable data) : DataSource
{
    public MethodTable Data => data;
}

public sealed class PointerToDataSource(DataSource source, int pointerSize) : DataSource
{
    public DataSource Source => source;
    public int PointerSize => pointerSize;
}

public sealed class RegisterDataSource(string name) : DataSource
{
    public string Name { get; } = name;
}