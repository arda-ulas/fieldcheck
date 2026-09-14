using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FieldCheck.Api.Data.Migrations;

/// <summary>
/// Hand-written T-SQL objects. The SQL lives in Data/Sql/*.sql (embedded resources) so it is
/// readable and reviewable as SQL, and versions with the schema through this migration.
/// Each object is executed as its own batch because CREATE PROCEDURE / CREATE VIEW must be the
/// first statement in a batch.
/// </summary>
[DbContext(typeof(FieldCheckDbContext))]
[Migration("20260914200000_TsqlObjects")]
public partial class TsqlObjects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(ReadSql("usp_GetOverdueAssets.sql"));
        migrationBuilder.Sql(ReadSql("vw_AssetInspectionSummary.sql"));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.vw_AssetInspectionSummary;");
        migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_GetOverdueAssets;");
    }

    private static string ReadSql(string fileName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().Single(n => n.EndsWith("." + fileName, StringComparison.Ordinal));
        using var stream = asm.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
