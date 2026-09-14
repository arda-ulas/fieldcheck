using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldCheck.Api.Data.Migrations;

/// <summary>
/// Hand-written T-SQL objects. The SQL lives in Data/Sql/*.sql (embedded resources) so it is
/// readable and reviewable as SQL, and versions with the schema through this migration.
/// Each object is executed as its own batch because CREATE PROCEDURE / CREATE VIEW must be the
/// first statement in a batch. The SQL is wrapped in EXEC(N'...') so it also works inside the
/// IF NOT EXISTS ... BEGIN/END block that `dotnet ef migrations script --idempotent` generates;
/// without the wrapper the idempotent script fails with "incorrect syntax" on Azure SQL.
/// </summary>
[DbContext(typeof(FieldCheckDbContext))]
[Migration("20260914200000_TsqlObjects")]
public partial class TsqlObjects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(AsDynamicBatch(ReadSql("usp_GetOverdueAssets.sql")));
        migrationBuilder.Sql(AsDynamicBatch(ReadSql("vw_AssetInspectionSummary.sql")));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_AssetInspectionSummary;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetOverdueAssets;");
    }

    private static string AsDynamicBatch(string sql) => "EXEC(N'" + sql.Replace("'", "''") + "');";

    private static string ReadSql(string fileName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().Single(n => n.EndsWith("." + fileName, StringComparison.Ordinal));
        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
