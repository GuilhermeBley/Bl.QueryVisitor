using System.Linq.Expressions;
using System.Text;

namespace Bl.QueryVisitor.MySql.Exceptions;

public class QueryException
    : Exception
{
    public string? Query { get; }
    public Expression Expression { get; }

    public QueryException(string message, Expression expression, string? query)
        : this(message, expression, query, null)
    {
    }

    public QueryException(string message, Expression expression, string? query, Exception? innerException) 
        : base(ParseMessage(message, expression, query, innerException), innerException)
    {
        Query = query;
        Expression = expression;
    }

    private static string ParseMessage(string message, Expression expression, string? query, Exception? innerException)
    {
        StringBuilder builder = new();

        builder.Append(message);
        builder.Append("\n---Error in expression:\n");
        builder.Append(expression.ToString().Trim('\n'));

        if (query is not null)
        {
            builder.Append("\n---Generated query:\n");
            builder.Append(query.Trim('\n'));
        }
        else
        {
            builder.Append("\n---Generated query: Failed to generate query.");
        }

        if (innerException is not null)
        {
            builder.Append("\n---Inner exception:\n");
            builder.Append(innerException.Message);
        }

        return builder.ToString();
    }
}
