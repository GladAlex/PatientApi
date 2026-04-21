using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PatientApi.Data;
using PatientApi.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQL Server
builder.Services.AddDbContext<PatientApiDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

// Services
builder.Services.AddScoped<IPatientService, PatientService>();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Patient API",
        Version = "v1",
        Description = "REST API for managing newborn patients. BirthDate search follows FHIR specification (https://www.hl7.org/fhir/search.html#date)."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Create db and execute migrations on start
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("DefaultConnection")!;

    // Create db if it doesn't exist
    EnsureDatabase(connectionString, logger);

    // Execute migrations
    var db = scope.ServiceProvider.GetRequiredService<PatientApiDbContext>();
    db.Database.Migrate();

    logger.LogInformation("Database ready.");
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Patient API v1");
    c.RoutePrefix = string.Empty;
});

app.MapControllers();

app.Run();

static void EnsureDatabase(string connectionString, ILogger logger)
{
    var builder = new SqlConnectionStringBuilder(connectionString);
    var databaseName = builder.InitialCatalog;   

     builder.InitialCatalog = "master";

    using var connection = new SqlConnection(builder.ConnectionString);
    connection.Open();

    using var cmd = connection.CreateCommand();
    cmd.CommandText =
     "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '" + databaseName + "') " +
     "BEGIN CREATE DATABASE [" + databaseName + "] END";
    cmd.ExecuteNonQuery();

    logger.LogInformation("Database '{Database}' ensured.", databaseName);
}
