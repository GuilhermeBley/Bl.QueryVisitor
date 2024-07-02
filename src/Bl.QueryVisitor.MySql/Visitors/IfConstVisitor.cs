using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.MySql.Visitors;

/// <summary>
/// this visitor will try execute an If clause and checks if its true or false.
/// </summary>
internal class IfConstVisitor
    : ExpressionVisitor
{
    private static ExpressionType[] convertibleExpression
        = new[]
        {
            ExpressionType.Convert,
            ExpressionType.Constant,
        };

    public bool? Result { get; private set; }
    public bool CanBeCompiled { get; private set; } = false;

    private IfConstVisitor() { }

    protected override Expression VisitBinary(BinaryExpression node)
    {

        if (!convertibleExpression.Contains(node.Left.NodeType) ||
            !convertibleExpression.Contains(node.Right.NodeType))
            return node;

        try
        {
            // Attempt to compile the method call expression
            var exp = Expression.Lambda(node).Compile();

            Result = exp.DynamicInvoke() as bool?;
            return node;
        }
        catch
        {
            CanBeCompiled = false;
            return node;
        }
    }

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        return base.Visit(node);
    }

    /// <summary>
    /// Checks if the expression can be executed in memory.
    /// If returns 'null', the expression can't be evaluated.
    /// </summary>
    public static bool? EvaluateIfExpression(Expression expression)
    {
        var visitor = new IfConstVisitor();
        visitor.Visit(expression);
        return visitor.Result;
    }
}
