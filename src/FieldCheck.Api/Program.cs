using FieldCheck.Api.Contracts;
using FieldCheck.Api.Data;
using FieldCheck.Api.Data.Seed;
using FieldCheck.Api.Storage;
using Azure.Storage.Blobs;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from user-secrets locally and App Service configuration in Azure.
// The fallback has no credentials and only exists so `dotnet ef` can build the model at design time.
var connectionString = builder.Configuration.GetConnectionString("FieldCheck")
    ?? "Server=localhost;Database=FieldCheck;TrustServerCertificate=True";

builder.Services.AddDbContext<FieldCheckDbContext>(o => o.UseSqlServer(connectionString));

// Blob storage: Azurite locally ("UseDevelopmentStorage=true" is not a secret), a real storage
// account connection string from App Service configuration in Azure.
builder.Services.AddSingleton<IPhotoStorage>(sp =>
{
    var cs = builder.Configuration.GetConnectionString("BlobStorage")
        ?? throw new InvalidOperationException("ConnectionStrings:BlobStorage is not configured.");
    var containerName = builder.Configuration["Storage:PhotoContainer"] ?? "inspection-photos";
    return new BlobPhotoStorage(new BlobContainerClient(cs, containerName));
});
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddOData(o => o
        .Select().Filter().OrderBy().Count().SetMaxTop(100)   // $expand deliberately not enabled
        .AddRouteComponents("odata", BuildEdmModel()))
    .AddOData(o => o.TimeZone = TimeZoneInfo.Utc);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<FieldCheckDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseODataRouteDebug();

    // Demo data for local exploration. Schema is applied deliberately with `dotnet ef database
    // update`; the app never migrates on startup.
    if (app.Configuration.GetValue<bool>("Seed:Demo"))
    {
        using var scope = app.Services.CreateScope();
        await DemoSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<FieldCheckDbContext>());
    }
}

// Unhandled exceptions and bare status codes both become RFC 9457 ProblemDetails responses.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
    private static IEdmModel BuildEdmModel()
    {
        var b = new ODataConventionModelBuilder();
        b.EntitySet<InspectionRecord>("Inspections");
        return b.GetEdmModel();
    }
}
