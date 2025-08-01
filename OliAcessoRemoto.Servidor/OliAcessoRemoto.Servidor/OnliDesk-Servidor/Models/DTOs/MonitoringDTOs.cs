namespace OliAcessoRemoto.Servidor.Models.DTOs;

public class DashboardStatsDto
{
    public int ActiveConnections { get; set; }
    public int TotalConnectionsToday { get; set; }
    public int TotalConnectionsWeek { get; set; }
    public int TotalConnectionsMonth { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double NetworkUsage { get; set; }
    public List<ConnectionsByHourDto> ConnectionsByHour { get; set; } = new();
    public List<TopCountryDto> TopCountries { get; set; } = new();
}

public class ConnectionsByHourDto
{
    public int Hour { get; set; }
    public int Count { get; set; }
}

public class TopCountryDto
{
    public string Country { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class ActiveConnectionDto
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public string Status { get; set; } = string.Empty;
    public long BytesTransferred { get; set; }
}

public class ConnectionHistoryDto
{
    public string Id { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public long BytesTransferred { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SystemInfoDto
{
    public string ServerName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public string OperatingSystem { get; set; } = string.Empty;
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public long TotalMemory { get; set; }
    public long AvailableMemory { get; set; }
    public double DiskUsage { get; set; }
    public List<NetworkInterfaceDto> NetworkInterfaces { get; set; } = new();
}

public class NetworkInterfaceDto
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public long BytesReceived { get; set; }
    public long BytesSent { get; set; }
    public bool IsActive { get; set; }
}