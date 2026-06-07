using FinWallet.Application;
using FinWallet.Infrastructure;
using FinWallet.Infrastructure.Persistence;
using FinWallet.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://34.35.119.207:9000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// Add services to the container.
builder.Services.AddControllers();

// Add OpenAPI / Swagger
builder.Services.AddOpenApi();

// Register application layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register exception middleware
builder.Services.AddTransient<ExceptionHandlingMiddleware>();

// Configure CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowFrontend", policy =>
//    {
//        policy.AllowAnyOrigin()
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});

var app = builder.Build();

// Enable global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Add simple fallback endpoint for developers to inspect routes if needed
}

// Disable HTTPS redirection for easier Docker deployment (SSL is offloaded)
// app.UseHttpsRedirection();

//app.UseCors("AllowFrontend");
app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run database seeding on startup
try
{
    app.Logger.LogInformation("Starting database migration and seeding...");
    await DatabaseSeeder.SeedDatabaseAsync(app.Services);
    app.Logger.LogInformation("Database migration and seeding completed successfully.");
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Failed to migrate and seed the database on startup.");
}

app.Run();
