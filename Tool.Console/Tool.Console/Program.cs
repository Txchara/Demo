using Tool.Core.NetTools;
using Tool.Core.SysTools;

namespace MainProcess;

public class Program
{
    public static async Task Main(string[] args)
    {
        var call = new Call();
        await call.Run();
    }
}

public class Call
{
    private bool _running = true;

    public async Task Run()
    {
        while (_running)
        {
            Console.WriteLine("输入 1 调用 PingTool");
            Console.WriteLine("输入 2 调用 SystemInfoTool");
            Console.WriteLine("输入 3 调用 TemperatureTool");
            Console.WriteLine("输入 4 调用 MacAddressTool");
            Console.WriteLine("输入 5 调用 LocalIpTool");
            Console.WriteLine("输入 0 退出程序");

            string? num = Console.ReadLine();

            Func<Task> action = num switch
            {
                "1" => UsePingTool,
                "2" => UseSystemInfoTool,
                "3" => UseTemperatureTool,
                "4" => UseMacAddressTool,
                "5" => UseLocalIpTool,
                "0" => Exit,
                _ => () =>
                {
                    RETURN(num);
                    return Task.CompletedTask;
                }
            };

            await action();
        }
    }

    /// <summary>
    /// 调用 PingTool 主体
    /// </summary>
    private async Task UsePingTool()
    {
        Console.Write("Please input address: ");
        string? address = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(address))
        {
            Console.WriteLine("Address is empty.");
            return;
        }

        var pingTool = new PingTool();
        PingResult result = await pingTool.PingAsync(address);

        Console.WriteLine();
        Console.WriteLine($"Address       : {result.Address}");
        Console.WriteLine($"Success       : {result.Success}");
        Console.WriteLine($"Status        : {result.Status}");
        Console.WriteLine($"IP Address    : {result.IpAddress ?? "-"}");
        Console.WriteLine($"Roundtrip(ms) : {result.RoundtripTime}");

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            Console.WriteLine($"Error         : {result.ErrorMessage}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// 调用 SystemInfoTool 主体
    /// </summary>
    private Task UseSystemInfoTool()
    {
        var systemInfoTool = new SystemInfoTool();
        SystemInfoResult result = systemInfoTool.GetSystemInfo();

        Console.WriteLine();
        Console.WriteLine("===== 系统信息 =====");
        Console.WriteLine($"CPU           : {result.CpuName}");
        Console.WriteLine($"Memory(GB)    : {result.TotalMemoryGb:F2}");
        Console.WriteLine($"OS Name       : {result.OsName}");
        Console.WriteLine($"OS Version    : {result.OsVersion}");

        Console.WriteLine();
        Console.WriteLine("GPU List:");
        if (result.GpuNames.Count == 0)
        {
            Console.WriteLine("- 未获取到 GPU 信息");
        }
        else
        {
            foreach (string gpu in result.GpuNames)
            {
                Console.WriteLine($"- {gpu}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Disk List:");
        if (result.Disks.Count == 0)
        {
            Console.WriteLine("- 未获取到磁盘信息");
        }
        else
        {
            foreach (DiskInfo disk in result.Disks)
            {
                Console.WriteLine($"- {disk.Name} [{disk.Format}] Total: {disk.TotalSizeGb:F2} GB, Free: {disk.FreeSizeGb:F2} GB");
            }
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 调用 TemperatureTool 主体
    /// </summary>
    private Task UseTemperatureTool()
    {
        var temperatureTool = new TemperatureTool();
        List<SensorReading> readings = temperatureTool.GetSensorReadings();

        Console.WriteLine();
        Console.WriteLine("===== 温度信息 =====");

        if (readings.Count == 0)
        {
            Console.WriteLine("未获取到温度数据。");
            Console.WriteLine();
            return Task.CompletedTask;
        }

        foreach (SensorReading reading in readings
                     .OrderBy(x => x.HardwareType)
                     .ThenBy(x => x.SensorName))
        {
            Console.WriteLine($"[{reading.HardwareType}] {reading.HardwareName} | {reading.SensorName} = {reading.Value:F2} {reading.Unit}");
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 调用 MacAddressTool 主体
    /// </summary>
    private Task UseMacAddressTool()
    {
        var macAddressTool = new MacAddressTool();
        List<MacAddressInfo> result = macAddressTool.GetMacAddresses();

        Console.WriteLine();
        Console.WriteLine("===== MAC 地址信息 =====");

        if (result.Count == 0)
        {
            Console.WriteLine("未获取到可用 MAC 地址");
            Console.WriteLine();
            return Task.CompletedTask;
        }

        foreach (MacAddressInfo item in result)
        {
            Console.WriteLine($"Name         : {item.Name}");
            Console.WriteLine($"Description  : {item.Description}");
            Console.WriteLine($"Type         : {item.InterfaceType}");
            Console.WriteLine($"MAC Address  : {item.MacAddress}");
            Console.WriteLine();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 调用 LocalIpTool 主体
    /// </summary>
    private Task UseLocalIpTool()
    {
        var localIpTool = new LocalIpTool();
        List<LocalIpInfo> result = localIpTool.GetLocalIps();

        Console.WriteLine();
        Console.WriteLine("===== 本机 IP 信息 =====");

        if (result.Count == 0)
        {
            Console.WriteLine("未获取到可用本机 IP");
            Console.WriteLine();
            return Task.CompletedTask;
        }

        foreach (LocalIpInfo item in result)
        {
            Console.WriteLine($"Name         : {item.Name}");
            Console.WriteLine($"Description  : {item.Description}");
            Console.WriteLine($"Type         : {item.InterfaceType}");
            Console.WriteLine($"Local IP     : {item.IpAddress}");
            Console.WriteLine();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 输入异常提示
    /// </summary>
    private static void RETURN(string? num)
    {
        if (string.IsNullOrWhiteSpace(num))
        {
            Console.WriteLine("不要用脚踩键盘");
        }
        else
        {
            Console.WriteLine($"你j8输入{num}什么意思？眼瞎？看清楚再输");
        }
    }

    /// <summary>
    /// 退出
    /// </summary>
    private Task Exit()
    {
        Console.WriteLine("程序已退出");
        _running = false;
        return Task.CompletedTask;
    }
}
