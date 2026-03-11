using Backend.Data;
using Backend.Services;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register Backend Services
builder.Services.AddTransient<ITmdbService, Backend.Services.TmdbService>();
builder.Services.AddTransient<IMediaAnalysisService, Backend.Services.MediaAnalysisService>();
builder.Services.AddTransient<IRenamerService, Backend.Services.RenamerService>();
builder.Services.AddTransient<ISubtitleService, Backend.Services.OpenSubtitlesService>();
builder.Services.AddTransient<ICleanupService, Backend.Services.CleanupService>();
builder.Services.AddSingleton<IFileSystemMonitorService, Backend.Services.FileSystemMonitorService>();
builder.Services.AddSingleton<IMediaProcessorService, Backend.Services.MediaProcessorService>();
builder.Services.AddSingleton<ITranscodingService, Backend.Services.TranscodingService>();

// Configure Entity Framework and PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure CORS for Vue frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.MapControllers();

app.MapControllers();

app.MapGet("/api/health", async (IServiceProvider provider) => 
{
    using var scope = provider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try 
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (canConnect) 
            return Results.Ok(new { Status = "Healthy" });
        else 
            return Results.StatusCode(503); // Return 503 instead of Ok, with custom status text handled by frontend if we want
    } 
    catch 
    {
        return Results.Problem(detail: "Database connection failed", title: "DB_OFFLINE", statusCode: 503);
    }
});

app.Run();
