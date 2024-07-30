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
    public void Translate_CheckOrderByWithThenBy_SuccessThenBy()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .ThenByDescending(model => model.Id)
            .OrderByDescending(model => model.InsertedAt)
            .ThenBy(model => model.Id);

        var visitor = new Visitors.SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal("\nORDER BY `InsertedAt` DESC, `Id` ASC, `Name` ASC, `Id` DESC", result.OrderBySql);
    }

    [Fact]
    public void Translate_CheckDoubleOrdering_SuccessLastOneAtFirstPlace()
    {
        var query = Enumerable.Empty<FakeModel>()
            .AsQueryable()
            .Where(model => model.Id == 1)
            .OrderBy(model => model.Name)
            .OrderBy(model => model.Id);

        var visitor = new Visitors.SimpleQueryTranslator();

        var translation = visitor.Translate(query.Expression);

        Assert.Equal("\nORDER BY `Id` ASC, `Name` ASC", translation.OrderBySql);
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
    public void Translate_CheckOrderByWithFalseTest_TestTranslatedToIf()
    {
        var query = FakeConnection.Default
            .SqlAsQueryable<FakeModel>(new CommandDefinition())
            .OrderBy(model => model.Name != null ? model.Name : null);

        var result = query.ToSqlText();

        Assert.Contains("\nORDER BY `Name` ASC", result);
    }

    [Fact]
    public void Translate_CheckSelectDistinctTypes_SuccessTypesCollectedButObject()
    {
        var query = Enumerable.Empty<FakeComplexModel>()
            .AsQueryable()
            .Select(c => new { c.MyGuid, c.MyObj, c.DateTimeOffset });

        var translator = new Visitors.SimpleQueryTranslator();

        var result = translator.Translate(query.Expression);

        Assert.Equal(
            new[] { nameof(FakeComplexModel.MyGuid), nameof(FakeComplexModel.DateTimeOffset) },
            result.Columns);
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
}