namespace OliAcessoRemoto.Servidor.Models;

public class ConnectionAttempt
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime AttemptTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string AttemptType { get; set; } = string.Empty; // "Direct", "Invitation", "Auto"
}

public class SystemMetrics
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public double NetworkIn { get; set; }
    public double NetworkOut { get; set; }
    public int ActiveConnections { get; set; }
    public int TotalConnections { get; set; }
}

public class ServerConfiguration
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
}