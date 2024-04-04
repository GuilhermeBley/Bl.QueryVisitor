using Bl.QueryVisitor.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections;
using System.Collections.Immutable;
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

        var whereVisitor = new InternalBinaryWhereClauseVisitor();
        var result = whereVisitor.Visit(node) as SqlExpression;

        _whereClause = result?.Print();

        return base.VisitMethodCall(node);
    }
    protected override Expression VisitExtension(Expression node)
    {
        return base.VisitExtension(node);
    }

    private class InternalBinaryWhereClauseVisitor
        : SqlExpressionVisitor
    {
        protected override Expression VisitCase(CaseExpression caseExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitCollate(CollateExpression collateExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitColumn(ColumnExpression columnExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitCrossApply(CrossApplyExpression crossApplyExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitCrossJoin(CrossJoinExpression crossJoinExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitDistinct(DistinctExpression distinctExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitExcept(ExceptExpression exceptExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitExists(ExistsExpression existsExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitFromSql(FromSqlExpression fromSqlExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitIn(InExpression inExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitInnerJoin(InnerJoinExpression innerJoinExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitIntersect(IntersectExpression intersectExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitLeftJoin(LeftJoinExpression leftJoinExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitLike(LikeExpression likeExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitOrdering(OrderingExpression orderingExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitOuterApply(OuterApplyExpression outerApplyExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitProjection(ProjectionExpression projectionExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitRowNumber(RowNumberExpression rowNumberExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitScalarSubquery(ScalarSubqueryExpression scalarSubqueryExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitSelect(SelectExpression selectExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitSqlBinary(SqlBinaryExpression sqlBinaryExpression)
        {
            return Visit(sqlBinaryExpression);
        }

        protected override Expression VisitSqlConstant(SqlConstantExpression sqlConstantExpression)
        {
            return Visit(sqlConstantExpression);
        }

        protected override Expression VisitSqlFragment(SqlFragmentExpression sqlFragmentExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitSqlFunction(SqlFunctionExpression sqlFunctionExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitSqlParameter(SqlParameterExpression sqlParameterExpression)
        {
            return Visit(sqlParameterExpression);
        }

        protected override Expression VisitSqlUnary(SqlUnaryExpression sqlUnaryExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitTable(TableExpression tableExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitTableValuedFunction(TableValuedFunctionExpression tableValuedFunctionExpression)
        {
            throw new NotImplementedException();
        }

        protected override Expression VisitUnion(UnionExpression unionExpression)
        {
            throw new NotImplementedException();
        }
    }
}