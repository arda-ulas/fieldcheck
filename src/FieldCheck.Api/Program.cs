using FieldCheck.Api.Data;
using FieldCheck.Api.Data.Seed;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from user-secrets locally and App Service configuration in Azure.
// The fallback has no credentials and only exists so `dotnet ef` can build the model at design time.
var connectionString = builder.Configuration.GetConnectionString("FieldCheck")
    ?? "Server=localhost;Database=FieldCheck;TrustServerCertificate=True";

builder.Services.AddDbContext<FieldCheckDbContext>(o => o.UseSqlServer(connectionString));
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

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

app.Run();

public partial class Program;
