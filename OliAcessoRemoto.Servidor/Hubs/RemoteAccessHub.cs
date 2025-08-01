using Microsoft.AspNetCore.SignalR;
using OliAcessoRemoto.Servidor.Services;

namespace OliAcessoRemoto.Servidor.Hubs;

public class RemoteAccessHub : Hub
{
    private readonly IConnectionService _connectionService;
    private readonly IMonitoringService _monitoringService;
    private readonly ILogger<RemoteAccessHub> _logger;

    public RemoteAccessHub(
        IConnectionService connectionService,
        IMonitoringService monitoringService,
        ILogger<RemoteAccessHub> logger)
    {
        _connectionService = connectionService;
        _monitoringService = monitoringService;
        _logger = logger;
    }

    public async Task RegisterClient(string clientId, string clientName, string operatingSystem, string version)
    {
        try
        {
            var httpContext = Context.GetHttpContext();
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";

            var connectionId = await _connectionService.RegisterClientAsync(clientId, clientName, ipAddress, userAgent);
            
            // Adicionar à grupo de clientes conectados
            await Groups.AddToGroupAsync(Context.ConnectionId, "ConnectedClients");
            
            // Notificar outros clientes sobre nova conexão
            await Clients.Others.SendAsync("ClientConnected", new
            {
                ClientId = clientId,
                ClientName = clientName,
                ConnectedAt = DateTime.UtcNow,
                IpAddress = ipAddress
            });

            // Notificar dashboard sobre atualização
            await Clients.Group("Dashboard").SendAsync("ConnectionUpdate");

            _logger.LogInformation("Cliente {ClientId} registrado com sucesso", clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar cliente {ClientId}", clientId);
            throw;
        }
    }

    public async Task RequestConnection(string targetClientId, string requesterId, string requesterName)
    {
        try
        {
            // Verificar se o cliente alvo está conectado
            var isTargetConnected = await _connectionService.IsClientConnectedAsync(targetClientId);
            
            if (!isTargetConnected)
            {
                await Clients.Caller.SendAsync("ConnectionRequestFailed", "Cliente alvo não está conectado");
                return;
            }

            // Enviar solicitação para o cliente alvo
            await Clients.Group("ConnectedClients").SendAsync("ConnectionRequest", new
            {
                RequesterId = requesterId,
                RequesterName = requesterName,
                TargetId = targetClientId,
                RequestTime = DateTime.UtcNow
            });

            // Registrar tentativa de conexão
            var httpContext = Context.GetHttpContext();
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            
            await _monitoringService.RecordConnectionAttemptAsync(requesterId, targetClientId, ipAddress, true);

            _logger.LogInformation("Solicitação de conexão enviada: {RequesterId} -> {TargetId}", requesterId, targetClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar solicitação de conexão");
            
            var httpContext = Context.GetHttpContext();
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            await _monitoringService.RecordConnectionAttemptAsync(requesterId, targetClientId, ipAddress, false, ex.Message);
            
            throw;
        }
    }

    public async Task RespondToConnectionRequest(string requesterId, bool approved, string? reason = null)
    {
        try
        {
            if (approved)
            {
                // Notificar ambos os clientes sobre aprovação
                await Clients.Group("ConnectedClients").SendAsync("ConnectionApproved", new
                {
                    RequesterId = requesterId,
                    TargetId = Context.UserIdentifier,
                    ApprovedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Conexão aprovada: {RequesterId} -> {TargetId}", requesterId, Context.UserIdentifier);
            }
            else
            {
                // Notificar solicitante sobre rejeição
                await Clients.Group("ConnectedClients").SendAsync("ConnectionRejected", new
                {
                    RequesterId = requesterId,
                    TargetId = Context.UserIdentifier,
                    Reason = reason ?? "Conexão rejeitada pelo usuário",
                    RejectedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Conexão rejeitada: {RequesterId} -> {TargetId}, Motivo: {Reason}", 
                    requesterId, Context.UserIdentifier, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao responder solicitação de conexão");
            throw;
        }
    }

    public async Task SendScreenData(string targetId, byte[] screenData, int width, int height)
    {
        try
        {
            // Enviar dados de tela para o cliente específico
            await Clients.Group("ConnectedClients").SendAsync("ScreenDataReceived", new
            {
                SourceId = Context.UserIdentifier,
                TargetId = targetId,
                ScreenData = screenData,
                Width = width,
                Height = height,
                Timestamp = DateTime.UtcNow
            });

            // Atualizar estatísticas
            await _connectionService.UpdateConnectionStatsAsync(Context.ConnectionId, 0, screenData.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar dados de tela");
            throw;
        }
    }

    public async Task SendInputEvent(string targetId, string eventType, object eventData)
    {
        try
        {
            // Enviar evento de entrada para o cliente específico
            await Clients.Group("ConnectedClients").SendAsync("InputEventReceived", new
            {
                SourceId = Context.UserIdentifier,
                TargetId = targetId,
                EventType = eventType,
                EventData = eventData,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogDebug("Evento de entrada enviado: {EventType} de {SourceId} para {TargetId}", 
                eventType, Context.UserIdentifier, targetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar evento de entrada");
            throw;
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Cliente conectado: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            // Remover do grupo
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "ConnectedClients");
            
            // Atualizar status da conexão
            await _connectionService.DisconnectClientAsync(Context.ConnectionId);
            
            // Notificar outros clientes
            await Clients.Others.SendAsync("ClientDisconnected", new
            {
                ConnectionId = Context.ConnectionId,
                DisconnectedAt = DateTime.UtcNow
            });

            // Notificar dashboard
            await Clients.Group("Dashboard").SendAsync("ConnectionUpdate");

            _logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar desconexão");
        }

        await base.OnDisconnectedAsync(exception);
    }
}