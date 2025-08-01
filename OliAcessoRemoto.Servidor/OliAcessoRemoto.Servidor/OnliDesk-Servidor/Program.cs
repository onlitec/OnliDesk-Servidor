using Microsoft.EntityFrameworkCore;
using OliAcessoRemoto.Servidor.Data;
using OliAcessoRemoto.Servidor.Hubs;
using OliAcessoRemoto.Servidor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/server-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("OliAcessoRemoto"));

// Services
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddSingleton<ISystemInfoService, SystemInfoService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapHub<RemoteAccessHub>("/remotehub");
app.MapHub<MonitoringHub>("/monitoringhub");

// Servir a interface web
app.MapFallbackToFile("index.html");

// Health check endpoint
app.MapGet("/health", () => new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0"
});

app.Run();
