using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bl.QueryVisitor.Visitors;

internal class HavingPerformanceImprovedFromSqlExpressionVisitor
    : ExpressionVisitor
{
    private readonly IAsyncQueryProvider _asyncQueryProvider;
    private readonly IEntityType _entityType;

    public HavingPerformanceImprovedFromSqlExpressionVisitor(
        IAsyncQueryProvider asyncQueryProvider,
        IEntityType entityType)
    {
        _asyncQueryProvider = asyncQueryProvider;
        _entityType = entityType;
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

        if ((Expression)selectExpression.Tables[0] is not FromSqlQueryRootExpression firstTable)
            return base.VisitExtension(selectExpression);

        return new InternExp(
            asyncQueryProvider: _asyncQueryProvider,
            entityType: _entityType,
            rootSql: firstTable,
            predicate: selectExpression.Predicate,
            orderBy: selectExpression.Orderings,
            limit: selectExpression.Limit);
    }

    private class InternExp : QueryRootExpression
    {
        private readonly QueryRootExpression _rootSql;
        private readonly SqlExpression? _predicate;
        private readonly IReadOnlyCollection<OrderingExpression> _orderBy;
        private readonly SqlExpression? _limit;

        public InternExp(
            IAsyncQueryProvider asyncQueryProvider, 
            IEntityType entityType,
            QueryRootExpression rootSql,
            SqlExpression? predicate,
            IReadOnlyCollection<OrderingExpression> orderBy,
            SqlExpression? limit)
            : base(asyncQueryProvider, entityType)
        {
            _rootSql = rootSql;
            _predicate = predicate;
            _orderBy = orderBy;
            _limit = limit;
        }

        protected override void Print(ExpressionPrinter expressionPrinter)
        {
            throw new NotImplementedException();
        }
    }
}
