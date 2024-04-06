﻿using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Pomelo.EntityFrameworkCore.MySql.Query.Internal;
using System.Linq.Expressions;

namespace Bl.QueryVisitor.Visitors;

internal class HavingPerformanceImprovedFromSqlExpressionVisitor
    : ExpressionVisitor
{
    public HavingPerformanceImprovedFromSqlExpressionVisitor()
    {
    }

    protected override Expression VisitExtension(Expression extensionExpression)
        => extensionExpression switch
        {
            SelectExpression selectExpression => VisitSelect(selectExpression),
            ShapedQueryExpression shapedQueryExpression => shapedQueryExpression.Update(
                Visit(shapedQueryExpression.QueryExpression), Visit(shapedQueryExpression.ShaperExpression)),
            _ => base.VisitExtension(extensionExpression)
        };

    protected virtual Expression VisitSelect(SelectExpression selectExpression)
    {
        var whereClauses = selectExpression.Predicate;

        if (whereClauses is null)
            return base.VisitExtension(selectExpression);

        if (selectExpression.Tables.Count != 2)
            return base.VisitExtension(selectExpression);

        if ((Expression)selectExpression.Tables[0] is not QueryRootExpression firstTable)
            return base.VisitExtension(selectExpression);

        var secondTable = (SelectExpression)selectExpression.Tables[1];

        throw new NotImplementedException();        
    }
}
