using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using OliAcessoRemoto.Servidor.Models.DTOs;

namespace OliAcessoRemoto.Servidor.Services;

public interface ISystemInfoService
{
    Task<SystemInfoDto> GetSystemInfoAsync();
    Task<double> GetCpuUsageAsync();
    Task<double> GetMemoryUsageAsync();
    Task<double> GetDiskUsageAsync();
    Task<List<NetworkInterfaceDto>> GetNetworkInterfacesAsync();
}

public class SystemInfoService : ISystemInfoService
{
    private readonly ILogger<SystemInfoService> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _memoryCounter;

    public SystemInfoService(ILogger<SystemInfoService> logger)
    {
        _logger = logger;
        InitializePerformanceCounters();
    }

    private void InitializePerformanceCounters()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível inicializar contadores de performance");
        }
    }

    public async Task<SystemInfoDto> GetSystemInfoAsync()
    {
        return new SystemInfoDto
        {
            ServerName = Environment.MachineName,
            Version = "1.0.0",
            StartTime = _startTime,
            Uptime = DateTime.UtcNow - _startTime,
            OperatingSystem = RuntimeInformation.OSDescription,
            CpuUsage = await GetCpuUsageAsync(),
            MemoryUsage = await GetMemoryUsageAsync(),
            TotalMemory = GC.GetTotalMemory(false),
            AvailableMemory = GetAvailableMemory(),
            DiskUsage = await GetDiskUsageAsync(),
            NetworkInterfaces = await GetNetworkInterfacesAsync()
        };
    }

    public async Task<double> GetCpuUsageAsync()
    {
        try
        {
            if (_cpuCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Primeira leitura para inicializar
                _cpuCounter.NextValue();
                await Task.Delay(100);
                return Math.Round(_cpuCounter.NextValue(), 2);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await GetLinuxCpuUsageAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter uso de CPU");
        }

        return 0.0;
    }

    public async Task<double> GetMemoryUsageAsync()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var totalMemory = GetTotalPhysicalMemory();
                var availableMemory = GetAvailableMemory();
                var usedMemory = totalMemory - availableMemory;
                return totalMemory > 0 ? Math.Round((double)usedMemory / totalMemory * 100, 2) : 0.0;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await GetLinuxMemoryUsageAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter uso de memória");
        }

        return 0.0;
    }

    public async Task<double> GetDiskUsageAsync()
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            var totalSize = drives.Sum(d => d.TotalSize);
            var totalFree = drives.Sum(d => d.TotalFreeSpace);
            var usedSpace = totalSize - totalFree;
            
            return totalSize > 0 ? Math.Round((double)usedSpace / totalSize * 100, 2) : 0.0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter uso de disco");
            return 0.0;
        }
    }

    public async Task<List<NetworkInterfaceDto>> GetNetworkInterfacesAsync()
    {
        var interfaces = new List<NetworkInterfaceDto>();

        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                           ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in networkInterfaces)
            {
                var stats = ni.GetIPv4Statistics();
                var properties = ni.GetIPProperties();
                var ipAddress = properties.UnicastAddresses
                    .FirstOrDefault(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.Address.ToString() ?? "N/A";

                interfaces.Add(new NetworkInterfaceDto
                {
                    Name = ni.Name,
                    IpAddress = ipAddress,
                    BytesReceived = stats.BytesReceived,
                    BytesSent = stats.BytesSent,
                    IsActive = ni.OperationalStatus == OperationalStatus.Up
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter interfaces de rede");
        }

        return interfaces;
    }

    private async Task<double> GetLinuxCpuUsageAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "top",
                Arguments = "-bn1 | grep \"Cpu(s)\" | sed \"s/.*, *\\([0-9.]*\\)%* id.*/\\1/\" | awk '{print 100 - $1}'",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (double.TryParse(output.Trim(), out var cpuUsage))
                {
                    return Math.Round(cpuUsage, 2);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter CPU usage no Linux");
        }

        return 0.0;
    }

    private async Task<double> GetLinuxMemoryUsageAsync()
    {
        try
        {
            var memInfo = await File.ReadAllTextAsync("/proc/meminfo");
            var lines = memInfo.Split('\n');
            
            var totalKb = ParseMemInfoLine(lines.FirstOrDefault(l => l.StartsWith("MemTotal:")));
            var availableKb = ParseMemInfoLine(lines.FirstOrDefault(l => l.StartsWith("MemAvailable:"))) ??
                             ParseMemInfoLine(lines.FirstOrDefault(l => l.StartsWith("MemFree:")));

            if (totalKb.HasValue && availableKb.HasValue)
            {
                var usedKb = totalKb.Value - availableKb.Value;
                return Math.Round((double)usedKb / totalKb.Value * 100, 2);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter memory usage no Linux");
        }

        return 0.0;
    }

    private long? ParseMemInfoLine(string? line)
    {
        if (string.IsNullOrEmpty(line)) return null;
        
        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && long.TryParse(parts[1], out var value))
        {
            return value;
        }
        
        return null;
    }

    private long GetTotalPhysicalMemory()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    return (long)memStatus.ullTotalPhys;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter memória total");
        }

        return 0;
    }

    private long GetAvailableMemory()
    {
        try
        {
            if (_memoryCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return (long)(_memoryCounter.NextValue() * 1024 * 1024); // Convert MB to bytes
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao obter memória disponível");
        }

        return 0;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }
}