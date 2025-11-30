using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace EntityFrameworkCore.UseRowNumberForPaging.Test;

public class SimpleTestCases
{
    [Fact]
    public void With_TrivialOk()
    {
        using (var dbContext = new UseRowNumberDbContext())
        {
            var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).OrderBy(i => i.BlogId).Skip(0).Take(10).ToQueryString();
            rawSql.ShouldContain("ROW_NUMBER");

            EfSqlHelpers.ValidateSql(rawSql);
        }
    }

    [Fact]
    public void Without_TrivialOk()
    {
        using (var dbContext = new NotUseRowNumberDbContext())
        {
            var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).Skip(0).Take(10).ToQueryString();
            rawSql.ShouldContain("OFFSET");
            rawSql.ShouldNotContain("ROW_NUMBER");

            EfSqlHelpers.ValidateSql(rawSql, "150");
        }
    }

    [Fact]
    public void With_NoSkipClause_OrderDesc_NoRowNumber()
    {
        using var dbContext = new UseRowNumberDbContext();
        var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).OrderByDescending(o => o.Rating).Take(20).ToQueryString();
        rawSql.ShouldNotContain("ROW_NUMBER");
        rawSql.ShouldContain("TOP");
        rawSql.ShouldContain("ORDER BY");

        EfSqlHelpers.ValidateSql(rawSql);
    }

    [Fact]
    public void With_OrderDesc_UsesRowNumber()
    {
        using var dbContext = new UseRowNumberDbContext();
        var rawSql = dbContext.Blogs.Where(i => i.BlogId > 1).OrderByDescending(o => o.Rating).Skip(20).Take(20).ToQueryString();
        rawSql.ShouldContain("ROW_NUMBER");
        rawSql.ShouldContain("ORDER BY");
        rawSql.ShouldContain("TOP");

        EfSqlHelpers.ValidateSql(rawSql);
    }

    [Fact]
    public void With_Order_SplitQuery_UsesRowNumber()
    {
        using var dbContext = new UseRowNumberDbContext();
        var rawSql = dbContext.Blogs.Include(b => b.Author).Where(i => i.BlogId > 1)
            .OrderBy(a => a.Author.ContributingSince)
            .OrderByDescending(o => o.Rating)
            .Skip(30).Take(15)
            .AsSplitQuery().ToQueryString();

        rawSql = EfSqlHelpers.StripEfSplitQueryComment(rawSql);

        rawSql.ShouldContain("ROW_NUMBER");
        rawSql.ShouldContain("ORDER BY");
        rawSql.ShouldContain("TOP");
        rawSql.ShouldNotContain("OFFSET");

        EfSqlHelpers.ValidateSql(rawSql);
    }

    [Fact]
    public void With_MultipleIncludes_UsesRowNumber_NoOffset()
    {
        using var dbContext = new UseRowNumberDbContext();
        var rawSql = dbContext.Blogs
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(i => i.BlogId > 1)
            .OrderBy(a => a.Author.ContributingSince)
            .OrderByDescending(o => o.Rating)
            .Skip(30).Take(15)
            .ToQueryString();

        rawSql.ShouldContain("ROW_NUMBER");
        rawSql.ShouldContain("ORDER BY");
        rawSql.ShouldContain("TOP");
        rawSql.ShouldNotContain("OFFSET");

        EfSqlHelpers.ValidateSql(rawSql);
    }

    [Fact]
    public void With_MultipleIncludes_SplitQuery_UsesRowNumber_NoOffset()
    {
        using var dbContext = new UseRowNumberDbContext();

        var rawSql = dbContext.Blogs
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.BlogId > 1)
            .OrderBy(a => a.Author.ContributingSince)
            .OrderByDescending(b => b.Rating)
            .Skip(30)
            .Take(15)
            .AsSplitQuery()
            .ToQueryString();

        rawSql = EfSqlHelpers.StripEfSplitQueryComment(rawSql);

        rawSql.ShouldContain("ROW_NUMBER");
        rawSql.ShouldNotContain("OFFSET");
        rawSql.ShouldContain("ORDER BY");
        rawSql.ShouldContain("TOP");

        EfSqlHelpers.ValidateSql(rawSql);
    }
}
