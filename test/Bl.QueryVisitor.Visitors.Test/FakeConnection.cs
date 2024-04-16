using System.Data;

namespace Bl.QueryVisitor.Visitors.Test;

internal class FakeConnection
    : IDbConnection
{
    private static NotImplementedException DefaultError
        = new NotImplementedException("The unique purpose of this connection, is to test.");

    public static IDbConnection Default { get; } = new FakeConnection();

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public string ConnectionString { get => throw DefaultError; set => throw DefaultError; }
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).

    public int ConnectionTimeout => throw DefaultError;

    public string Database => throw DefaultError;

    public ConnectionState State => ConnectionState.Closed;

    private FakeConnection() { }

    public IDbTransaction BeginTransaction()
    {
        throw DefaultError;
    }

    public IDbTransaction BeginTransaction(IsolationLevel il)
    {
        throw DefaultError;
    }

    public void ChangeDatabase(string databaseName)
    {
    }

    public void Close()
    {
    }

    public IDbCommand CreateCommand()
    {
        throw DefaultError;
    }

    public void Dispose()
    {
    }

    public void Open()
    {
    }
}
