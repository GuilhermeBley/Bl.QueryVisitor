using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.Visitors;

public class MySqlNullSimplifier
    : ExpressionVisitor
{
    private bool _translated;
    
    public MySqlNullSimplifier() { }

    protected override Expression VisitConditional(
        ConditionalExpression node)
    {
        if (node.Test is not BinaryExpression binaryExp)
            return base.VisitConditional(node);

        if (!IsMemberOrParameter(binaryExp.Left))
            return base.VisitConditional(node);

        if (binaryExp.NodeType == ExpressionType.Equal 
            && IsNullConstant(binaryExp.Right)
            && IsNullConstant(node.IfTrue)
            && IsMemberOrParameter(node.IfFalse))
        {
            _translated = true;
            return node.IfFalse;
        }
        else if (binaryExp.NodeType == ExpressionType.NotEqual 
            && IsNullConstant(binaryExp.Right)
            && IsNullConstant(node.IfFalse)
            && IsMemberOrParameter(node.IfTrue))
        {
            _translated = true;
            return node.IfTrue;
        }
        
        return base.VisitConditional(node);
    }

    private static bool IsMemberOrParameter(Expression exp)
        => exp.NodeType == ExpressionType.Parameter
        || exp.NodeType == ExpressionType.MemberAccess;

    protected bool IsNullConstant(Expression exp)
    {
        return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
    }
}
