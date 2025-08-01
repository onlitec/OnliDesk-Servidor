using Microsoft.AspNetCore.Mvc;
using OliAcessoRemoto.Servidor.Services;

namespace OliAcessoRemoto.Servidor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;
    private readonly IConnectionService _connectionService;
    private readonly ISystemInfoService _systemInfoService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IMonitoringService monitoringService,
        IConnectionService connectionService,
        ISystemInfoService systemInfoService,
        ILogger<DashboardController> logger)
    {
        _monitoringService = monitoringService;
        _connectionService = connectionService;
        _systemInfoService = systemInfoService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém estatísticas do dashboard
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        try
        {
            var stats = await _monitoringService.GetDashboardStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas do dashboard");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém informações do sistema
    /// </summary>
    [HttpGet("system-info")]
    public async Task<IActionResult> GetSystemInfo()
    {
        try
        {
            var systemInfo = await _systemInfoService.GetSystemInfoAsync();
            return Ok(systemInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do sistema");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém conexões ativas
    /// </summary>
    [HttpGet("active-connections")]
    public async Task<IActionResult> GetActiveConnections()
    {
        try
        {
            var connections = await _connectionService.GetActiveConnectionsAsync();
            return Ok(connections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter conexões ativas");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém histórico de conexões
    /// </summary>
    [HttpGet("connection-history")]
    public async Task<IActionResult> GetConnectionHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var history = await _connectionService.GetConnectionHistoryAsync(page, pageSize);
            return Ok(new { data = history, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter histórico de conexões");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Força desconexão de um cliente
    /// </summary>
    [HttpPost("disconnect/{connectionId}")]
    public async Task<IActionResult> DisconnectClient(string connectionId)
    {
        try
        {
            await _connectionService.DisconnectClientAsync(connectionId);
            return Ok(new { message = "Cliente desconectado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desconectar cliente {ConnectionId}", connectionId);
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém métricas do sistema em um período
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetSystemMetrics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddHours(-24);
            var toDate = to ?? DateTime.UtcNow;

            var metrics = await _monitoringService.GetSystemMetricsAsync(fromDate, toDate);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter métricas do sistema");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }
}