using Microsoft.EntityFrameworkCore;
using OliAcessoRemoto.Servidor.Data;
using OliAcessoRemoto.Servidor.Models;
using OliAcessoRemoto.Servidor.Models.DTOs;

namespace OliAcessoRemoto.Servidor.Services;

public interface IMonitoringService
{
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task RecordSystemMetricsAsync();
    Task<List<SystemMetrics>> GetSystemMetricsAsync(DateTime from, DateTime to);
    Task RecordConnectionAttemptAsync(string clientId, string targetId, string ipAddress, bool success, string? errorMessage = null);
}

public class MonitoringService : IMonitoringService
{
    private readonly ApplicationDbContext _context;
    private readonly ISystemInfoService _systemInfoService;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(
        ApplicationDbContext context, 
        ISystemInfoService systemInfoService,
        ILogger<MonitoringService> logger)
    {
        _context = context;
        _systemInfoService = systemInfoService;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddMonths(-1);

        var activeConnections = await _context.ClientConnections
            .CountAsync(c => c.IsActive);

        var connectionsToday = await _context.ClientConnections
            .CountAsync(c => c.ConnectedAt >= today);

        var connectionsWeek = await _context.ClientConnections
            .CountAsync(c => c.ConnectedAt >= weekAgo);

        var connectionsMonth = await _context.ClientConnections
            .CountAsync(c => c.ConnectedAt >= monthAgo);

        var systemInfo = await _systemInfoService.GetSystemInfoAsync();

        // Conexões por hora (últimas 24 horas)
        var connectionsByHour = await _context.ClientConnections
            .Where(c => c.ConnectedAt >= today)
            .GroupBy(c => c.ConnectedAt.Hour)
            .Select(g => new ConnectionsByHourDto
            {
                Hour = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Hour)
            .ToListAsync();

        // Top países
        var topCountries = await _context.ClientConnections
            .Where(c => c.ConnectedAt >= weekAgo && !string.IsNullOrEmpty(c.Country))
            .GroupBy(c => c.Country)
            .Select(g => new { Country = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var totalCountries = topCountries.Sum(x => x.Count);

        return new DashboardStatsDto
        {
            ActiveConnections = activeConnections,
            TotalConnectionsToday = connectionsToday,
            TotalConnectionsWeek = connectionsWeek,
            TotalConnectionsMonth = connectionsMonth,
            CpuUsage = systemInfo.CpuUsage,
            MemoryUsage = systemInfo.MemoryUsage,
            NetworkUsage = systemInfo.NetworkInterfaces.Sum(n => n.BytesReceived + n.BytesSent),
            ConnectionsByHour = connectionsByHour,
            TopCountries = topCountries.Select(x => new TopCountryDto
            {
                Country = x.Country ?? "Unknown",
                Count = x.Count,
                Percentage = totalCountries > 0 ? (double)x.Count / totalCountries * 100 : 0
            }).ToList()
        };
    }

    public async Task RecordSystemMetricsAsync()
    {
        try
        {
            var systemInfo = await _systemInfoService.GetSystemInfoAsync();
            var activeConnections = await _context.ClientConnections.CountAsync(c => c.IsActive);
            var totalConnections = await _context.ClientConnections.CountAsync();

            var metrics = new SystemMetrics
            {
                Timestamp = DateTime.UtcNow,
                CpuUsage = systemInfo.CpuUsage,
                MemoryUsage = systemInfo.MemoryUsage,
                DiskUsage = systemInfo.DiskUsage,
                NetworkIn = systemInfo.NetworkInterfaces.Sum(n => n.BytesReceived),
                NetworkOut = systemInfo.NetworkInterfaces.Sum(n => n.BytesSent),
                ActiveConnections = activeConnections,
                TotalConnections = totalConnections
            };

            _context.SystemMetrics.Add(metrics);
            await _context.SaveChangesAsync();

            // Limpar métricas antigas (manter apenas últimos 30 dias)
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var oldMetrics = await _context.SystemMetrics
                .Where(m => m.Timestamp < cutoffDate)
                .ToListAsync();

            if (oldMetrics.Any())
            {
                _context.SystemMetrics.RemoveRange(oldMetrics);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar métricas do sistema");
        }
    }

    public async Task<List<SystemMetrics>> GetSystemMetricsAsync(DateTime from, DateTime to)
    {
        return await _context.SystemMetrics
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task RecordConnectionAttemptAsync(string clientId, string targetId, string ipAddress, bool success, string? errorMessage = null)
    {
        var attempt = new ConnectionAttempt
        {
            ClientId = clientId,
            TargetId = targetId,
            IpAddress = ipAddress,
            AttemptTime = DateTime.UtcNow,
            Success = success,
            ErrorMessage = errorMessage,
            AttemptType = "Direct"
        };

        _context.ConnectionAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Tentativa de conexão registrada: {ClientId} -> {TargetId}, Sucesso: {Success}", 
            clientId, targetId, success);
    }
}