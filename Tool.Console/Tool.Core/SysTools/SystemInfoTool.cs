using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Tool.Core.SysTools;

/// <summary>
/// 系统信息采集工具。
/// </summary>
public sealed class SystemInfoTool
{
    /// <summary>
    /// 一次性读取当前计算机的核心系统信息。
    /// </summary>
    public SystemInfoResult GetSystemInfo()
    {
        return new SystemInfoResult
        {
            CpuName = GetCpuName(),
            GpuNames = GetGpuNames(),
            TotalMemoryGb = GetTotalMemoryGb(),
            OsName = GetOsName(),
            OsVersion = GetOsVersion(),
            Disks = GetDiskInfos()
        };
    }

    private static string GetCpuName()
    {
        string? cpuName = TryQueryFirstPropertyValue(
            "SELECT Name FROM Win32_Processor",
            "Name");

        if (!string.IsNullOrWhiteSpace(cpuName))
        {
            return cpuName;
        }

        if (OperatingSystem.IsWindows())
        {
            object? registryValue = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                "ProcessorNameString",
                null);

            if (registryValue is string registryCpuName &&
                !string.IsNullOrWhiteSpace(registryCpuName))
            {
                return registryCpuName.Trim();
            }

            string? envCpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
            if (!string.IsNullOrWhiteSpace(envCpuName))
            {
                return envCpuName.Trim();
            }
        }

        return $"{RuntimeInformation.ProcessArchitecture} CPU";
    }

    private static List<string> GetGpuNames()
    {
        return TryQueryPropertyValues(
            "SELECT Name FROM Win32_VideoController",
            "Name");
    }

    private static double GetTotalMemoryGb()
    {
        string? totalPhysicalMemory = TryQueryFirstPropertyValue(
            "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem",
            "TotalPhysicalMemory");

        if (double.TryParse(totalPhysicalMemory, out double bytes))
        {
            return Math.Round(bytes / 1024d / 1024 / 1024, 2);
        }

        long fallbackBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        if (fallbackBytes > 0)
        {
            return Math.Round(fallbackBytes / 1024d / 1024 / 1024, 2);
        }

        return 0;
    }

    private static string GetOsName()
    {
        string? osName = TryQueryFirstPropertyValue(
            "SELECT Caption FROM Win32_OperatingSystem",
            "Caption");

        return string.IsNullOrWhiteSpace(osName)
            ? RuntimeInformation.OSDescription.Trim()
            : osName;
    }

    private static string GetOsVersion()
    {
        string? osVersion = TryQueryFirstPropertyValue(
            "SELECT Version FROM Win32_OperatingSystem",
            "Version");

        return string.IsNullOrWhiteSpace(osVersion)
            ? Environment.OSVersion.VersionString
            : osVersion;
    }

    private static List<DiskInfo> GetDiskInfos()
    {
        var result = new List<DiskInfo>();

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }

            result.Add(new DiskInfo
            {
                Name = drive.Name,
                Format = drive.DriveFormat,
                TotalSizeGb = Math.Round(drive.TotalSize / 1024d / 1024 / 1024, 2),
                FreeSizeGb = Math.Round(drive.AvailableFreeSpace / 1024d / 1024 / 1024, 2)
            });
        }

        return result;
    }

    private static string? TryQueryFirstPropertyValue(string query, string propertyName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject obj in searcher.Get())
            {
                string? value = obj[propertyName]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (ManagementException)
        {
        }
        catch (COMException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }

    private static List<string> TryQueryPropertyValues(string query, string propertyName)
    {
        var result = new List<string>();

        if (!OperatingSystem.IsWindows())
        {
            return result;
        }

        try
        {
            using var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementObject obj in searcher.Get())
            {
                string? value = obj[propertyName]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(value) &&
                    !result.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(value);
                }
            }
        }
        catch (PlatformNotSupportedException)
        {
        }
        catch (ManagementException)
        {
        }
        catch (COMException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return result;
    }
}

/// <summary>
/// 系统信息聚合结果。
/// </summary>
public sealed class SystemInfoResult
{
    public string CpuName { get; init; } = string.Empty;

    public List<string> GpuNames { get; init; } = new();

    public double TotalMemoryGb { get; init; }

    public string OsName { get; init; } = string.Empty;

    public string OsVersion { get; init; } = string.Empty;

    public List<DiskInfo> Disks { get; init; } = new();
}

/// <summary>
/// 单个磁盘的容量信息。
/// </summary>
public sealed class DiskInfo
{
    public string Name { get; init; } = string.Empty;

    public string Format { get; init; } = string.Empty;

    public double TotalSizeGb { get; init; }

    public double FreeSizeGb { get; init; }
}
