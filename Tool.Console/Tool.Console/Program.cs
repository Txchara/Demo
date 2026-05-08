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
            Console.WriteLine("输入 0 退出程序");

            string? num = Console.ReadLine();

            Func<Task> action = num switch
            {
                "1" => UsePingTool,
                "2" => UseSystemInfoTool,
                "3" => UseTemperatureTool,
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
    /// <returns></returns>
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
    }

    /// <summary>
    /// 调用 SystemInfoTool 主体
    /// </summary>
    /// <returns></returns>
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
    ///  调用 TemperatureTool 主体
    /// </summary>
    /// <returns></returns>
    private Task UseTemperatureTool()
    {
        var temperatureTool = new TemperatureTool();
        List<SensorReading> readings = temperatureTool.GetSensorReadings();

        Console.WriteLine();
        Console.WriteLine("===== 硬件传感器信息 =====");

        if (readings.Count == 0)
        {
            Console.WriteLine("未获取到传感器数据。");
            Console.WriteLine("可能原因：当前机器不支持、未开启对应传感器、权限不足，或当前硬件没有暴露这些信息。");
            Console.WriteLine();
            return Task.CompletedTask;
        }

        foreach (SensorReading reading in readings
                     .OrderBy(x => x.HardwareType)
                     .ThenBy(x => x.Category)
                     .ThenBy(x => x.SensorName))
        {
            Console.WriteLine(
                $"[{reading.HardwareType}] {reading.HardwareName} | {reading.Category} | {reading.SensorName} = {reading.Value:F2} {reading.Unit}");
        }

        Console.WriteLine();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 输入异常提示
    /// </summary>
    /// <param name="num"></param>
    private static void RETURN(string? num)
    {
        if (string.IsNullOrWhiteSpace(num)) Console.WriteLine("不要用脚踩键盘");
        else Console.WriteLine($"你j8输入{num}什么意思？眼瞎？看清楚再输");
    }

    /// <summary>
    /// 退出
    /// </summary>
    /// <returns></returns>
    private Task Exit()
    {
        Console.WriteLine("程序已退出");
        _running = false;
        return Task.CompletedTask;
    }
}
