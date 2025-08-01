using System.ComponentModel.DataAnnotations;

namespace OliAcessoRemoto.Servidor.Models;

public class ClientConnection
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    public string ClientId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public bool IsActive { get; set; }
    
    public string ConnectionType { get; set; } = string.Empty; // "Viewer", "Host"
    public string Status { get; set; } = string.Empty; // "Connected", "Disconnected", "Connecting"
    
    // Estatísticas da sessão
    public long BytesReceived { get; set; }
    public long BytesSent { get; set; }
    public int ScreenUpdatesCount { get; set; }
    public int InputEventsCount { get; set; }
    
    // Informações geográficas
    public string? Country { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}