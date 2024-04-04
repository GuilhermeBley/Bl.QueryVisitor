using System.Linq.Expressions;

namespace Bl.QueryVisitor.Expressions;

public class MySqlBinaryExpression
    : Expression,
    IPrintable
{
    public string Print()
    {
        if (this is not BinaryExpression binaryExpression)
            return string.Empty;
    }

    public override string ToString()
    {
        return Print();
    }
}
