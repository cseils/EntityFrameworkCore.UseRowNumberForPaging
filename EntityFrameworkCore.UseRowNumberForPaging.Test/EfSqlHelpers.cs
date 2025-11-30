using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace EntityFrameworkCore.UseRowNumberForPaging.Test;

public static class EfSqlHelpers
{
    private const string SplitQueryMarker =
        "This LINQ query is being executed in split-query mode";

    public static string StripEfSplitQueryComment(string sql)
    {
        if (string.IsNullOrEmpty(sql)) return sql;

        var idx = sql.IndexOf(SplitQueryMarker, StringComparison.Ordinal);
        if (idx < 0) return sql;

        return sql.Substring(0, idx).TrimEnd();
    }

    public static void ValidateSql(string sql, string compatibilityLevel = "100")
    {
        TSqlParser parser = compatibilityLevel switch
        {
            "100" => new TSql100Parser(initialQuotedIdentifiers: true),
            "110" => new TSql110Parser(initialQuotedIdentifiers: true),
            "120" => new TSql120Parser(initialQuotedIdentifiers: true),
            "130" => new TSql130Parser(initialQuotedIdentifiers: true),
            "140" => new TSql140Parser(initialQuotedIdentifiers: true),
            "150" => new TSql150Parser(initialQuotedIdentifiers: true),
            _ => throw new ArgumentException($"Unsupported compatibility level: {compatibilityLevel}")
        };
        IList<ParseError> errors;

        using var reader = new StringReader(sql);
        parser.Parse(reader, out errors);

        if (errors != null && errors.Count > 0)
        {
            var message = string.Join("\n", errors.Select(e =>
                $"Line {e.Line}, Col {e.Column}: {e.Message}"));

            throw new Exception("Invalid T-SQL:\n" + message);
        }
    }
}