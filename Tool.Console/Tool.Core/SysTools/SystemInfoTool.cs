using System.Management;

namespace Tool.Core.SysTools;

/// <summary>
/// 系统信息采集工具。
/// </summary>
public sealed class SystemInfoTool
{
    /// <summary>
    /// 一次性读取当前计算机的核心系统信息
    /// 返回对象中包含 CPU、GPU、内存、系统名称、系统版本以及磁盘信息
    /// </summary>
    /// <returns>系统信息聚合结果</returns>
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

    /// <summary>
    /// 获取 CPU 名称。
    /// </summary>
    /// <returns>CPU 名称；读取失败时返回“未知 CPU”</returns>
    private static string GetCpuName()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");

        foreach (ManagementObject obj in searcher.Get())
        {
            // 通常只需要第一个处理器名称即可满足展示需求
            return obj["Name"]?.ToString()?.Trim() ?? "未知 CPU";
        }

        return "未知 CPU";
    }

    /// <summary>
    /// 获取 GPU 列表
    /// 有些机器可能同时存在集成显卡和独立显卡，因此这里返回集合
    /// </summary>
    /// <returns>GPU 名称列表；如果没有读到结果则返回空集合</returns>
    private static List<string> GetGpuNames()
    {
        var result = new List<string>();
        using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");

        foreach (ManagementObject obj in searcher.Get())
        {
            string? name = obj["Name"]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                // 只保留有效名称，避免把空字符串加入结果集
                result.Add(name);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取总物理内存大小，单位为 GB
    /// </summary>
    /// <returns>总内存大小，保留两位小数；读取失败时返回 0</returns>
    private static double GetTotalMemoryGb()
    {
        using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");

        foreach (ManagementObject obj in searcher.Get())
        {
            if (obj["TotalPhysicalMemory"] is not null &&
                double.TryParse(obj["TotalPhysicalMemory"].ToString(), out double bytes))
            {
                // 将字节转换为 GB，便于在控制台或界面中直接展示
                return Math.Round(bytes / 1024d / 1024 / 1024, 2);
            }
        }

        return 0;
    }

    /// <summary>
    /// 获取操作系统名称。
    /// </summary>
    /// <returns>操作系统名称；读取失败时返回“未知系统”</returns>
    private static string GetOsName()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");

        foreach (ManagementObject obj in searcher.Get())
        {
            return obj["Caption"]?.ToString()?.Trim() ?? "未知系统";
        }

        return "未知系统";
    }

    /// <summary>
    /// 获取操作系统版本号
    /// </summary>
    /// <returns>系统版本号；读取失败时返回“未知版本”</returns>
    private static string GetOsVersion()
    {
        using var searcher = new ManagementObjectSearcher("SELECT Version FROM Win32_OperatingSystem");

        foreach (ManagementObject obj in searcher.Get())
        {
            return obj["Version"]?.ToString()?.Trim() ?? "未知版本";
        }

        return "未知版本";
    }

    /// <summary>
    /// 获取当前机器可用磁盘信息
    /// 这里使用 DriveInfo 而不是 WMI，原因是 DriveInfo 对磁盘容量读取更直接
    /// </summary>
    /// <returns>已就绪磁盘的信息列表</returns>
    private static List<DiskInfo> GetDiskInfos()
    {
        var result = new List<DiskInfo>();

        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady)
            {
                // 未就绪的盘符可能是光驱、未挂载设备或暂不可访问设备，直接跳过
                continue;
            }

            result.Add(new DiskInfo
            {
                Name = drive.Name,
                Format = drive.DriveFormat,
                // 统一转换为 GB，和内存展示格式保持一致
                TotalSizeGb = Math.Round(drive.TotalSize / 1024d / 1024 / 1024, 2),
                FreeSizeGb = Math.Round(drive.AvailableFreeSpace / 1024d / 1024 / 1024, 2)
            });
        }

        return result;
    }
}

/// <summary>
/// 系统信息聚合结果
/// 这个对象用于承接 SystemInfoTool 读取到的所有信息
/// </summary>
public sealed class SystemInfoResult
{
    /// <summary>
    /// CPU 名称。
    /// </summary>
    public string CpuName { get; init; } = string.Empty;

    /// <summary>
    /// GPU 名称列表。
    /// 机器可能存在多个显卡，因此使用集合承载。
    /// </summary>
    public List<string> GpuNames { get; init; } = new();

    /// <summary>
    /// 总物理内存大小，单位 GB。
    /// </summary>
    public double TotalMemoryGb { get; init; }

    /// <summary>
    /// 操作系统名称。
    /// </summary>
    public string OsName { get; init; } = string.Empty;

    /// <summary>
    /// 操作系统版本号。
    /// </summary>
    public string OsVersion { get; init; } = string.Empty;

    /// <summary>
    /// 磁盘信息列表。
    /// </summary>
    public List<DiskInfo> Disks { get; init; } = new();
}

/// <summary>
/// 单个磁盘的容量信息
/// </summary>
public sealed class DiskInfo
{
    /// <summary>
    /// 盘符名称，例如 C:\ 或 D:\。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 文件系统格式，例如 NTFS、FAT32。
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    /// 磁盘总容量，单位 GB。
    /// </summary>
    public double TotalSizeGb { get; init; }

    /// <summary>
    /// 磁盘可用空间，单位 GB。
    /// </summary>
    public double FreeSizeGb { get; init; }
}
