using Microsoft.EntityFrameworkCore;
using OliAcessoRemoto.Servidor.Data;
using OliAcessoRemoto.Servidor.Models;
using OliAcessoRemoto.Servidor.Models.DTOs;

namespace OliAcessoRemoto.Servidor.Services;

public interface IConnectionService
{
    Task<List<ActiveConnectionDto>> GetActiveConnectionsAsync();
    Task<List<ConnectionHistoryDto>> GetConnectionHistoryAsync(int page = 1, int pageSize = 50);
    Task<ClientConnection?> GetConnectionAsync(string connectionId);
    Task<string> RegisterClientAsync(string clientId, string clientName, string ipAddress, string userAgent);
    Task UpdateConnectionStatsAsync(string connectionId, long bytesReceived, long bytesSent);
    Task DisconnectClientAsync(string connectionId);
    Task<bool> IsClientConnectedAsync(string clientId);
}

public class ConnectionService : IConnectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConnectionService> _logger;

    public ConnectionService(ApplicationDbContext context, ILogger<ConnectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ActiveConnectionDto>> GetActiveConnectionsAsync()
    {
        return await _context.ClientConnections
            .Where(c => c.IsActive)
            .Select(c => new ActiveConnectionDto
            {
                Id = c.Id,
                ClientId = c.ClientId,
                ClientName = c.ClientName,
                IpAddress = c.IpAddress,
                Country = c.Country ?? "Unknown",
                ConnectionType = c.ConnectionType,
                ConnectedAt = c.ConnectedAt,
                Duration = DateTime.UtcNow - c.ConnectedAt,
                Status = c.Status,
                BytesTransferred = c.BytesReceived + c.BytesSent
            })
            .OrderByDescending(c => c.ConnectedAt)
            .ToListAsync();
    }

    public async Task<List<ConnectionHistoryDto>> GetConnectionHistoryAsync(int page = 1, int pageSize = 50)
    {
        return await _context.ClientConnections
            .OrderByDescending(c => c.ConnectedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConnectionHistoryDto
            {
                Id = c.Id,
                ClientId = c.ClientId,
                ClientName = c.ClientName,
                IpAddress = c.IpAddress,
                ConnectedAt = c.ConnectedAt,
                DisconnectedAt = c.DisconnectedAt,
                Duration = c.DisconnectedAt.HasValue ? c.DisconnectedAt.Value - c.ConnectedAt : null,
                BytesTransferred = c.BytesReceived + c.BytesSent,
                Status = c.Status
            })
            .ToListAsync();
    }

    public async Task<ClientConnection?> GetConnectionAsync(string connectionId)
    {
        return await _context.ClientConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId);
    }

    public async Task<string> RegisterClientAsync(string clientId, string clientName, string ipAddress, string userAgent)
    {
        var connectionId = Guid.NewGuid().ToString();
        
        var connection = new ClientConnection
        {
            Id = connectionId,
            ClientId = clientId,
            ClientName = clientName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ConnectedAt = DateTime.UtcNow,
            IsActive = true,
            Status = "Connected",
            ConnectionType = "Host" // Default, pode ser alterado depois
        };

        _context.ClientConnections.Add(connection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cliente {ClientId} conectado com ID {ConnectionId}", clientId, connectionId);
        
        return connectionId;
    }

    public async Task UpdateConnectionStatsAsync(string connectionId, long bytesReceived, long bytesSent)
    {
        var connection = await _context.ClientConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId);

        if (connection != null)
        {
            connection.BytesReceived += bytesReceived;
            connection.BytesSent += bytesSent;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DisconnectClientAsync(string connectionId)
    {
        var connection = await _context.ClientConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId);

        if (connection != null)
        {
            connection.IsActive = false;
            connection.DisconnectedAt = DateTime.UtcNow;
            connection.Status = "Disconnected";
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cliente {ClientId} desconectado", connection.ClientId);
        }
    }

    public async Task<bool> IsClientConnectedAsync(string clientId)
    {
        return await _context.ClientConnections
            .AnyAsync(c => c.ClientId == clientId && c.IsActive);
    }
}