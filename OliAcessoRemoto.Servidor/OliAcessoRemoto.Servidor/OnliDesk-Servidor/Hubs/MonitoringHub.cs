using Microsoft.AspNetCore.SignalR;
using OliAcessoRemoto.Servidor.Services;

namespace OliAcessoRemoto.Servidor.Hubs;

public class MonitoringHub : Hub
{
    private readonly IMonitoringService _monitoringService;
    private readonly IConnectionService _connectionService;
    private readonly ISystemInfoService _systemInfoService;
    private readonly ILogger<MonitoringHub> _logger;

    public MonitoringHub(
        IMonitoringService monitoringService,
        IConnectionService connectionService,
        ISystemInfoService systemInfoService,
        ILogger<MonitoringHub> logger)
    {
        _monitoringService = monitoringService;
        _connectionService = connectionService;
        _systemInfoService = systemInfoService;
        _logger = logger;
    }

    public async Task JoinDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation("Dashboard conectado: {ConnectionId}", Context.ConnectionId);
        
        // Enviar dados iniciais
        await SendDashboardUpdate();
    }

    public async Task LeaveDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation("Dashboard desconectado: {ConnectionId}", Context.ConnectionId);
    }

    public async Task RequestDashboardUpdate()
    {
        await SendDashboardUpdate();
    }

    public async Task RequestSystemInfo()
    {
        try
        {
            var systemInfo = await _systemInfoService.GetSystemInfoAsync();
            await Clients.Caller.SendAsync("SystemInfoUpdate", systemInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do sistema");
            await Clients.Caller.SendAsync("Error", "Erro ao obter informações do sistema");
        }
    }

    public async Task RequestActiveConnections()
    {
        try
        {
            var activeConnections = await _connectionService.GetActiveConnectionsAsync();
            await Clients.Caller.SendAsync("ActiveConnectionsUpdate", activeConnections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter conexões ativas");
            await Clients.Caller.SendAsync("Error", "Erro ao obter conexões ativas");
        }
    }

    public async Task DisconnectClient(string connectionId)
    {
        try
        {
            await _connectionService.DisconnectClientAsync(connectionId);
            
            // Notificar o cliente específico para desconectar
            await Clients.All.SendAsync("ForceDisconnect", connectionId);
            
            // Atualizar dashboard
            await SendDashboardUpdate();
            
            _logger.LogInformation("Cliente {ConnectionId} desconectado via dashboard", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desconectar cliente {ConnectionId}", connectionId);
            await Clients.Caller.SendAsync("Error", $"Erro ao desconectar cliente: {ex.Message}");
        }
    }

    private async Task SendDashboardUpdate()
    {
        try
        {
            var dashboardStats = await _monitoringService.GetDashboardStatsAsync();
            await Clients.Group("Dashboard").SendAsync("DashboardUpdate", dashboardStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar atualização do dashboard");
        }
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Monitoring client conectado: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Dashboard");
        _logger.LogInformation("Monitoring client desconectado: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}