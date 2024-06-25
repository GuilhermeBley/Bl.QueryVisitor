namespace Bl.QueryVisitor.Api.Test;

public class MySqlOptions
{
    public string ConnectionString { get; set; }
        = "server=127.0.0.1;port=3310;user id=root;password=root;persistsecurityinfo=True;database=queryable-test;default command timeout=600;SslMode=None";
}
