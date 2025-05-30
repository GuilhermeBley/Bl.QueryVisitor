﻿using Bl.QueryVisitor.Visitors;
using Bl.QueryVisitor.Visitors.Test;

namespace Bl.QueryVisitor.MySql.Visitors.Test.Test;

public class ConditionalTests
{
    [Fact]
    public void SimpleQueryTranslator_ShouldMatchPropertiesComparing()
    {
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => m.IsTrue == m.IsTrueNullable);

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrue` = `IsTrueNullable`", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldMatchBoolPropertyWithConst()
    {
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => m.IsTrue == true);

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrue` = @P1000", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldMatchBoolPropertyWithConstNullableBool()
    {
        bool? constNullableBool = null;
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => m.IsTrue == constNullableBool);

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrue` IS NULL", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldMatchBoolPropertyWithEqualConstNullableBool()
    {
        bool? constNullableBool = null;
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => m.IsTrue.Equals(constNullableBool));

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrue` IS NULL", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldMatchNullableBoolWithEquals()
    {
        bool? constNullableBool = null;
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => m.IsTrueNullable.Equals(constNullableBool));

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrueNullable` IS NULL", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldMatchNullableBoolWithStaticEquals()
    {
        bool? constNullableBool = null;
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => object.Equals(m.IsTrueNullable, constNullableBool));

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrueNullable` IS NULL", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldMatchNullableBoolWithEqualsTrueBool()
    {
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => m.IsTrueNullable == true);

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrueNullable` = @P1000", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldMatchNullableBoolPropertyWithConst()
    {
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => m.IsTrueNullable == true);

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrueNullable` = @P1000", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldMatchNullableBoolPropertyWithToString()
    {
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => m.IsTrueNullable.Equals(true.ToString()));

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("`IsTrueNullable` = @P1000", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldCheckBooleanNull()
    {
        bool? myBool = null;
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => (m.Value < 20) == myBool);

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Contains("(`Value` < @P1000) IS NULL", result.HavingSql);
    }

    [Fact]
    public void SimpleQueryTranslator_ShouldCompareUnaryExpWithPossibleNullableBooleans()
    {
        var other = new FakeBooleanModel()
        {
            Value = 23
        };
        bool? trueValue = true;
        var query = Enumerable.Empty<FakeBooleanModel>()
            .AsQueryable()
            .Where(m => (m.Value <= other.Value) == trueValue);

        var visitor = new SimpleQueryTranslator();

        var result = visitor.Translate(query.Expression);

        Assert.Equal(other.Value, result.Parameters.Values.FirstOrDefault());
        Assert.Contains("`Value` <= @P1000", result.HavingSql);
    }

    private class FakeBooleanModel
    {
        public bool IsTrue { get; set; }
        public bool? IsTrueNullable { get; set; }
        public decimal? Value { get; set; }
    }
}
