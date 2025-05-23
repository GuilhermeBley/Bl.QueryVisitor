using Bl.QueryVisitor.Extension;
using Dapper;

namespace Bl.QueryVisitor.Visitors.Test;

public class SimpleQueryTranslatorTest
{
    [Fact]
    public void Translate_TryGetWhereConstClauses_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1 && model.Name == "asc" || model.Name == "asc213")
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.NotNull(result);
    }

    [Fact]
    public void Translate_TryGetWhereVariablesClauses_Success()
    {
        var time = new DateTime(1900, 1, 1);

        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == time)
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var queryString = visitor.Translate(query.Expression).HavingSql;

        Assert.NotEmpty(queryString);
    }

    [Fact]
    public void Translate_TryGetWhereFunctionsClauses_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == new DateTime(1900, 1, 1))
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var queryString = visitor.Translate(query.Expression).HavingSql;

        Assert.Equal("\nHAVING (`InsertedAt` = @P1000)", queryString, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_TryGetWhereFunctionsClausesCheckNewDateTimeData_Success()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.InsertedAt == new DateTime(1900, 1, 1))
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(new DateTime(1900, 1, 1), result.Parameters.First().Value);
    }

    [Fact]
    public void Translate_CheckEmptyWhere_SuccessEmpty()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(string.Empty, result.HavingSql);
    }

    [Fact]
    public void Translate_CheckSelect_SuccessColumnsCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Select(model => new { model.Name });

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Single(result.Columns);
    }

    [Fact]
    public void Translate_CheckSelectWithOtherExpressions_SuccessColumnsCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .Skip(100)
            .Take(100)
            .Select(model => new { model.Name }); ;

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.NotEmpty(result.HavingSql);
        Assert.NotEmpty(result.OrderBySql);
        Assert.NotEmpty(result.LimitSql);
        Assert.Single(result.Columns);
    }

    [Fact]
    public void Translate_CheckSelectWithouNewObject_SuccessColumnsCollected()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Select(model =>  model.Name);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Single(result.Columns);
    }

    [Fact]
    public void Translate_CheckEmptyOrder_SuccessEmpty()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .Skip(100)
            .Take(100);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(string.Empty, result.OrderBySql);
    }

    [Fact]
    public void Translate_CheckEmptyLimit_SuccessEmpty()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(string.Empty, result.LimitSql);
    }

    [Fact]
    public void Translate_CheckThenByAfterOrder_SuccessThenBy()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .ThenBy(model => model.Id)
            .ThenByDescending(model => model.InsertedAt);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nORDER BY `Name` ASC, `Id` ASC, `InsertedAt` DESC", result.OrderBySql);
    }

    [Fact]
    public void Translate_ShouldOrderByBeReorderedByInsertedAt()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .OrderByDescending(model => model.InsertedAt)
            .ThenBy(model => model.Id);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nORDER BY `InsertedAt` DESC, `Id` ASC", result.OrderBySql);
    }

    [Fact]
    public void Translate_ShouldPassIfReorderedById()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .OrderBy(model => model.Id);

        var visitor = new Visitors.SimpleQueryTranslator();

        var translation = visitor.Translate(query.Expression);

        Assert.Equal("\nORDER BY `Id` ASC", translation.OrderBySql);
    }

    [Fact]
    public void Translate_ShouldReorderWorksWithDuplicatedSorts()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .ThenBy(model => model.Id)
            .OrderBy(model => model.Id)
            .ThenBy(model => model.InsertedAt);

        var visitor = new Visitors.SimpleQueryTranslator();

        var translation = visitor.Translate(query.Expression);

        Assert.Equal("\nORDER BY `Id` ASC, `InsertedAt` ASC", translation.OrderBySql);
    }

    [Fact]
    public void Translate_CheckLimitText_SuccessLimitFound()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .Take(1);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nLIMIT 1", result.LimitSql);
    }

    [Fact]
    public void Translate_CheckLimitOffsetText_SuccessLimitFound()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .Take(1)
            .Skip(1);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nLIMIT 1 OFFSET 1", result.LimitSql);
    }

    [Fact]
    public void Translate_CheckSetColumnNameOnWhere_SuccessWhereColumnNameEdited()
    {
        const string renamedColumn = "newName";

        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .Where(model => model.Name == "name")
            .SetColumnName(model => model.Name, renamedColumn)
            .Skip(1);

        var result = query.ToSqlText();

        Assert.Contains(renamedColumn, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_CheckSetColumnNameOnOrderBy_SuccessWhereColumnNameEdited()
    {
        const string renamedColumnName = "newName";
        const string renamedColumnId = "newId";

        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .OrderBy(model => model.Name)
            .ThenBy(model => model.Id)
            .SetColumnName(model => model.Name, renamedColumnName)
            .SetColumnName(model => model.Id, renamedColumnId)
            .Skip(1);

        var result = query.ToSqlText();

        Assert.Contains(renamedColumnName, result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(renamedColumnId, result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Translate_CheckOrderByWithTest_TestTranslatedToIf()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .OrderBy(model => model.Name == null ? null : model.Name);

        var result = query.ToSqlText();

        Assert.Contains("\nORDER BY `Name` ASC", result);
    }

    [Fact]
    public void Translate_CheckSelectedColumns()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .Select(model =>
                new
                {
                    model.Name
                });

        var result = query.ToSqlText();

        Assert.Contains("SELECT `t0`.`Name` FROM", result);
    }

    [Fact]
    public void Translate_CheckSelectedColumnsWithConversion()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .AddConversion(e =>
            {
                // fake conversion
            })
            .Select(model =>
            new
            {
                model.Name
            });

        var result = query.ToSqlText();

        Assert.Contains("SELECT `t0`.`Name` FROM", result);
    }

    [Fact]
    public void Translate_CheckOrderByWithFalseTest_TestTranslatedToIf()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .OrderBy(model => model.Name != null ? model.Name : null);

        var result = query.ToSqlText();

        Assert.Contains("\nORDER BY `Name` ASC", result);
    }

    [Fact]
    public void Translate_CheckSelectWithUnderlineType_SuccessTypesCollected()
    {
        var query = Enumerable.Empty<FakeComplexModel>()
            .AsQueryable()
            .Select(c => new { c.DateTimeOffsetWithUnderlineType });

        var translator = new Visitors.SimpleQueryTranslator();

        var result = translator.Translate(query.Expression);

        Assert.Equal(
            new[] { nameof(FakeComplexModel.DateTimeOffsetWithUnderlineType) },
            result.Columns);
    }

    [Fact]
    public void Translate_CheckDoubleWhere_SuccessQueryGenerated()
    {
        var query = Enumerable.Empty<FakeComplexModel>()
            .AsQueryable()
            .Where(f => f.MyGuid == new Guid("337f04a8-c8cb-488c-b64d-e0ecdcc07977"))
            .Where(f => f.DateTimeOffset == new DateTime(2004, 1, 1));

        var translator = new Visitors.SimpleQueryTranslator();

        var result = translator.Translate(query.Expression);

        Assert.Equal("\nHAVING (`MyGuid` = @P1000) AND (`DateTimeOffset` = @P1001)", result.HavingSql);
    }

    [Fact]
    public void Translate_CheckTripleWhere_SuccessQueryGenerated()
    {
        var query = Enumerable.Empty<FakeComplexModel>()
            .AsQueryable()
            .Where(f => f.MyGuid == new Guid("337f04a8-c8cb-488c-b64d-e0ecdcc07977"))
            .Where(f => f.MyGuid == new Guid("337f04a8-c8cb-488c-b64d-e0ecdcc07977"))
            .Where(f => f.DateTimeOffset == new DateTime(2004, 1, 1));

        var translator = new Visitors.SimpleQueryTranslator();

        var result = translator.Translate(query.Expression);

        Assert.Equal("\nHAVING (`MyGuid` = @P1000) AND (`MyGuid` = @P1001) AND (`DateTimeOffset` = @P1002)", result.HavingSql);
    }

    [Fact]
    public void EnsureAllColumnSet_CheckIfSelectPropertyMatch()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("FROM Users"))
            .AsQueryable()
            .EnsureAllColumnSet()
            .SetColumnName(e => e.InsertedAt, "IF(InsertedAt > 0, InsertedAt, NULL) Date")
            .SetColumnName(e => e.Name, "NULL")
            .SetColumnName(e => e.Id, "f.Id");

        var result = query.ToSqlText();

        Assert.Contains("SELECT\nf.Id AS `Id`,\nIF(InsertedAt > 0, InsertedAt, NULL) Date AS `InsertedAt`,\nNULL AS `Name` FROM Users;", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureAllColumnSet_ShouldColumnsInsertedAtAndIdBeRemovedFromTheSql()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("FROM Users"))
            .AsQueryable()
            .EnsureAllColumnSet()
            .SetColumnName(e => e.InsertedAt, "IF(InsertedAt > 0, InsertedAt, NULL) Date")
            .SetColumnName(e => e.Name, "NULL")
            .SetColumnName(e => e.Id, "f.Id")
            .Select(e => new { e.Name });

        var result = query.ToSqlText();

        Assert.DoesNotContain("IF(InsertedAt > 0, InsertedAt, NULL) Date", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("f.Id", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureAllColumnSet_ShouldWriteWhereWithHiddenColumns()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("FROM Users"))
            .AsQueryable()
            .EnsureAllColumnSet()
            .SetColumnName(e => e.InsertedAt, "IF(InsertedAt > 0, InsertedAt, NULL) Date")
            .SetColumnName(e => e.Name, "NULL")
            .SetColumnName(e => e.Id, "f.Id")
            .Where(e => e.InsertedAt > new DateTime(2005, 01, 01))
            .Select(e => new { e.Name });

        var result = query.ToSqlText();

        Assert.Contains("WHERE (IF(InsertedAt > 0, InsertedAt, NULL) Date > @", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureAllColumnSet_CheckIfOrderByAddedSpecificColumn()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("FROM User"))
            .AsQueryable()
            .EnsureAllColumnSet()
            .SetColumnName(e => e.InsertedAt, "IF(InsertedAt > 0, InsertedAt, NULL)")
            .SetColumnName(e => e.Name, "NULL")
            .SetColumnName(e => e.Id, "f.Id")
            .OrderBy(e => e.Id);

        var result = query.ToSqlText();

        Assert.Contains("ORDER BY f.Id ASC", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureAllColumnSet_CheckIfWhereAddedSpecificColumn()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("FROM User"))
            .AsQueryable()
            .EnsureAllColumnSet()
            .SetColumnName(e => e.InsertedAt, "IF(InsertedAt > 0, InsertedAt, NULL)")
            .SetColumnName(e => e.Name, "NULL")
            .SetColumnName(e => e.Id, "f.Id")
            .Where(e => e.InsertedAt > new DateTime(2025, 1, 1));

        var result = query.ToSqlText();

        Assert.Contains("WHERE (IF(InsertedAt > 0, InsertedAt, NULL) > @", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSql_CheckIfSqlWasAdddedHeader()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .AsQueryable()
            .AddSql(MySql.CommandLocaleRegion.Header, "SET NAMES = 'latin';");

        var result = query.ToSqlText();

        Assert.StartsWith("\nSET NAMES = 'latin';\n", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSql_ShouldMatchGroupBy()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("SELECT * FROM table1"))
            .AsQueryable()
            .AddSql(MySql.CommandLocaleRegion.BeforeHavingSelection, "GROUP BY p.Id");

        var result = query.ToSqlText();

        Assert.Contains("SELECT * FROM table1\nGROUP BY p.Id\n", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSql_ShouldMatchGroupByWithHaving()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("SELECT * FROM table1"))
            .AsQueryable()
            .Where(e => e.Id == 1)
            .AddSql(MySql.CommandLocaleRegion.BeforeHavingSelection, "GROUP BY p.Id");

        var result = query.ToSqlText();

        Assert.Contains("SELECT * FROM table1\nGROUP BY p.Id\n\nHAVING (`Id` = @", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSql_ShouldMatchGroupByWithWhere()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("SELECT * FROM table1"))
            .AsQueryable()
            .SetColumnName(e => e.Id, "p.Id")
            .EnsureAllColumnSet()
            .Where(e => e.Id == 1)
            .AddSql(MySql.CommandLocaleRegion.BeforeHavingSelection, "GROUP BY p.Id");

        var result = query.ToSqlText();

        Assert.Contains("SELECT * FROM table1\nWHERE (p.Id = @P1000)\nGROUP BY p.Id\n", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrderBySql_ShouldPersistTheFirstValueIfDuplicated()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .OrderBy(e => e.InsertedAt)
            .ThenByDescending(e => e.InsertedAt);

        var result = query.ToSqlText();

        Assert.Contains("ORDER BY `InsertedAt` ASC;", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrderBySql_ShouldMatchWithMultipleClausesAndOrder()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition("SELECT Id From table.table"))
            .Where(e => e.InsertedAt > new DateTime(1900, 1, 1))
            .OrderBy(e => e.InsertedAt)
            .ThenByDescending(e => e.InsertedAt)
            .Where(e => e.Id == 12)
            .OrderBy(e => e.Id)
            .Skip(10)
            .Take(100);

        var result = query.ToSqlText();

        string[] matches = [
            "(`InsertedAt` > @P1000) AND (`Id` = @P1001)",
            "ORDER BY `Id` ASC",
            "LIMIT 100 OFFSET 10",
        ];

        Assert.All(matches, m =>
            Assert.Contains(m, result, StringComparison.OrdinalIgnoreCase));
    }
}