using Microsoft.AspNetCore.Mvc;
using OliAcessoRemoto.Servidor.Services;

namespace OliAcessoRemoto.Servidor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Gera relatório de conexões
    /// </summary>
    [HttpGet("connections")]
    public async Task<IActionResult> GenerateConnectionReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string format = "csv")
    {
        try
        {
            if (from >= to)
            {
                return BadRequest(new { error = "Data inicial deve ser menor que data final" });
            }

            var report = await _reportService.GenerateConnectionReportAsync(from, to, format);
            var fileName = $"connections_report_{from:yyyyMMdd}_{to:yyyyMMdd}.{format}";
            
            return File(report, GetContentType(format), fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de conexões");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Gera relatório de métricas do sistema
    /// </summary>
    [HttpGet("system-metrics")]
    public async Task<IActionResult> GenerateSystemMetricsReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string format = "csv")
    {
        try
        {
            if (from >= to)
            {
                return BadRequest(new { error = "Data inicial deve ser menor que data final" });
            }

            var report = await _reportService.GenerateSystemMetricsReportAsync(from, to, format);
            var fileName = $"system_metrics_report_{from:yyyyMMdd}_{to:yyyyMMdd}.{format}";
            
            return File(report, GetContentType(format), fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar relatório de métricas");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém estatísticas detalhadas
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
            var toDate = to ?? DateTime.UtcNow;

            if (fromDate >= toDate)
            {
                return BadRequest(new { error = "Data inicial deve ser menor que data final" });
            }

            var statistics = await _reportService.GetStatisticsAsync(fromDate, toDate);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas");
            return StatusCode(500, new { error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obtém relatórios disponíveis
    /// </summary>
    [HttpGet("available")]
    public IActionResult GetAvailableReports()
    {
        var reports = new[]
        {
            new
            {
                id = "connections",
                name = "Relatório de Conexões",
                description = "Relatório detalhado de todas as conexões realizadas",
                formats = new[] { "csv" },
                endpoint = "/api/reports/connections"
            },
            new
            {
                id = "system-metrics",
                name = "Relatório de Métricas do Sistema",
                description = "Relatório de performance e uso de recursos do sistema",
                formats = new[] { "csv" },
                endpoint = "/api/reports/system-metrics"
            },
            new
            {
                id = "statistics",
                name = "Estatísticas Gerais",
                description = "Estatísticas consolidadas do sistema",
                formats = new[] { "json" },
                endpoint = "/api/reports/statistics"
            }
        };

        return Ok(reports);
    }

    private static string GetContentType(string format)
    {
        return format.ToLower() switch
        {
            "csv" => "text/csv",
            "json" => "application/json",
            "pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}