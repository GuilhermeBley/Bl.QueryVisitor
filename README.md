# Bl.QueryVisitor

Project provides extensions to be used with Dapper to manipulate SQL commands with LINQ.

## How to use the feature

The follow LINQ method:

```csharp
var queryable =
    CreateConnection()
    .SqlAsQueryable<FakeModel>(new CommandDefinition(
        "FROM `queryable-test`.FakeModel a"))
    .SetColumnName(e => e.Id, "a.Id")
    .SetColumnName(e => e.InsertedAt, "a.InsertedAt")
    .SetColumnName(e => e.InsertedAtOnlyDate, "a.InsertedAtOnlyDate")
    .SetColumnName(e => e.Name, "a.Name")
    .SetColumnName(e => e.Value, "a.Value")
    .EnsureAllColumnSet()

    .Where(x => x.Name == "FakeName")
    .Select(x => new { x.Id })
    .OrderBy(x => x.Id)
    
    .ToList();
```

will be translated into:

```sql
SELECT a.Id AS `Id` FROM `queryable-test`.FakeModel a
WHERE (a.Name = @P1000)
ORDER BY a.Id ASC
LIMIT 1;
```

All of that using just DAPPER package.
