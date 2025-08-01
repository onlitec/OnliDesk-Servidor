using Microsoft.EntityFrameworkCore;
using OliAcessoRemoto.Servidor.Data;
using OliAcessoRemoto.Servidor.Models;
using System.Text;

namespace OliAcessoRemoto.Servidor.Services;

public interface IReportService
{
    Task<byte[]> GenerateConnectionReportAsync(DateTime from, DateTime to, string format = "csv");
    Task<byte[]> GenerateSystemMetricsReportAsync(DateTime from, DateTime to, string format = "csv");
    Task<Dictionary<string, object>> GetStatisticsAsync(DateTime from, DateTime to);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> GenerateConnectionReportAsync(DateTime from, DateTime to, string format = "csv")
    {
        var connections = await _context.ClientConnections
            .Where(c => c.ConnectedAt >= from && c.ConnectedAt <= to)
            .OrderByDescending(c => c.ConnectedAt)
            .ToListAsync();

        if (format.ToLower() == "csv")
        {
            return GenerateConnectionsCsv(connections);
        }

        throw new NotSupportedException($"Formato {format} não suportado");
    }

    public async Task<byte[]> GenerateSystemMetricsReportAsync(DateTime from, DateTime to, string format = "csv")
    {
        var metrics = await _context.SystemMetrics
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        if (format.ToLower() == "csv")
        {
            return GenerateMetricsCsv(metrics);
        }

        throw new NotSupportedException($"Formato {format} não suportado");
    }

    public async Task<Dictionary<string, object>> GetStatisticsAsync(DateTime from, DateTime to)
    {
        var connections = await _context.ClientConnections
            .Where(c => c.ConnectedAt >= from && c.ConnectedAt <= to)
            .ToListAsync();

        var attempts = await _context.ConnectionAttempts
            .Where(a => a.AttemptTime >= from && a.AttemptTime <= to)
            .ToListAsync();

        var metrics = await _context.SystemMetrics
            .Where(m => m.Timestamp >= from && m.Timestamp <= to)
            .ToListAsync();

        var totalConnections = connections.Count;
        var successfulConnections = connections.Count(c => c.Status == "Connected" || c.DisconnectedAt.HasValue);
        var totalAttempts = attempts.Count;
        var successfulAttempts = attempts.Count(a => a.Success);
        
        var avgSessionDuration = connections
            .Where(c => c.DisconnectedAt.HasValue)
            .Select(c => (c.DisconnectedAt!.Value - c.ConnectedAt).TotalMinutes)
            .DefaultIfEmpty(0)
            .Average();

        var totalDataTransferred = connections.Sum(c => c.BytesReceived + c.BytesSent);

        var topCountries = connections
            .Where(c => !string.IsNullOrEmpty(c.Country))
            .GroupBy(c => c.Country)
            .Select(g => new { Country = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var connectionsByDay = connections
            .GroupBy(c => c.ConnectedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        var avgCpuUsage = metrics.Any() ? metrics.Average(m => m.CpuUsage) : 0;
        var avgMemoryUsage = metrics.Any() ? metrics.Average(m => m.MemoryUsage) : 0;
        var maxConcurrentConnections = metrics.Any() ? metrics.Max(m => m.ActiveConnections) : 0;

        return new Dictionary<string, object>
        {
            ["period"] = new { from, to },
            ["connections"] = new
            {
                total = totalConnections,
                successful = successfulConnections,
                successRate = totalConnections > 0 ? (double)successfulConnections / totalConnections * 100 : 0,
                avgSessionDuration = Math.Round(avgSessionDuration, 2),
                totalDataTransferred = totalDataTransferred
            },
            ["attempts"] = new
            {
                total = totalAttempts,
                successful = successfulAttempts,
                successRate = totalAttempts > 0 ? (double)successfulAttempts / totalAttempts * 100 : 0
            },
            ["geography"] = new
            {
                topCountries = topCountries,
                uniqueCountries = topCountries.Count
            },
            ["timeline"] = new
            {
                connectionsByDay = connectionsByDay
            },
            ["performance"] = new
            {
                avgCpuUsage = Math.Round(avgCpuUsage, 2),
                avgMemoryUsage = Math.Round(avgMemoryUsage, 2),
                maxConcurrentConnections = maxConcurrentConnections
            }
        };
    }

    private byte[] GenerateConnectionsCsv(List<ClientConnection> connections)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("ID,ClientID,ClientName,IPAddress,Country,ConnectionType,ConnectedAt,DisconnectedAt,Duration,BytesReceived,BytesSent,Status");

        // Data
        foreach (var connection in connections)
        {
            var duration = connection.DisconnectedAt.HasValue 
                ? (connection.DisconnectedAt.Value - connection.ConnectedAt).ToString(@"hh\:mm\:ss")
                : "N/A";

            csv.AppendLine($"{connection.Id},{connection.ClientId},{connection.ClientName},{connection.IpAddress}," +
                          $"{connection.Country},{connection.ConnectionType},{connection.ConnectedAt:yyyy-MM-dd HH:mm:ss}," +
                          $"{connection.DisconnectedAt?.ToString("yyyy-MM-dd HH:mm:ss")},{duration}," +
                          $"{connection.BytesReceived},{connection.BytesSent},{connection.Status}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    private byte[] GenerateMetricsCsv(List<SystemMetrics> metrics)
    {
        var csv = new StringBuilder();
        
        // Header
        csv.AppendLine("Timestamp,CpuUsage,MemoryUsage,DiskUsage,NetworkIn,NetworkOut,ActiveConnections,TotalConnections");

        // Data
        foreach (var metric in metrics)
        {
            csv.AppendLine($"{metric.Timestamp:yyyy-MM-dd HH:mm:ss},{metric.CpuUsage},{metric.MemoryUsage}," +
                          $"{metric.DiskUsage},{metric.NetworkIn},{metric.NetworkOut}," +
                          $"{metric.ActiveConnections},{metric.TotalConnections}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}