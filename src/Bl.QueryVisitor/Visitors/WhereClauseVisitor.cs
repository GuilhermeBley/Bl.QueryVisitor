using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Pomelo.EntityFrameworkCore.MySql.Query.Expressions.Internal;
using Pomelo.EntityFrameworkCore.MySql.Query.ExpressionVisitors.Internal;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Linq.Expressions;
using System.Text;

namespace Bl.QueryVisitor.Visitors;

public class WhereClauseVisitor : ExpressionVisitor
{
    private string? _whereClause;

    public string GetWhereClauses(Expression expression)
    {
        Visit(expression);

        return _whereClause ?? string.Empty;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name != "Where")
            return base.VisitMethodCall(node);
        throw new NotImplementedException();
    }
    protected override Expression VisitExtension(Expression node)
    {
        return base.VisitExtension(node);
    }
}