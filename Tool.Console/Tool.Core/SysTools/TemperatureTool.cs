using LibreHardwareMonitor.Hardware;

namespace Tool.Core.SysTools;

/// <summary>
/// 硬件传感器读取工具。
/// 当前版本先只展示温度相关参数，其他传感器类型保留注释，后续可直接恢复。
/// </summary>
public class TemperatureTool
{
    /// <summary>
    /// 读取当前传感器的值。
    /// </summary>
    public List<SensorReading> GetSensorReadings()
    {
        var result = new List<SensorReading>();

        // 打开可能包含温度传感器的硬件类别。
        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true
        };

        try
        {
            computer.Open();
            computer.Accept(new UpdateVisitor());

            foreach (IHardware hardware in computer.Hardware)
            {
                ReadHardwareSensors(hardware, result);
            }

            return result;
        }
        finally
        {
            computer.Close();
        }
    }

    /// <summary>
    /// 递归读取硬件和子硬件上的传感器。
    /// </summary>
    private static void ReadHardwareSensors(IHardware hardware, List<SensorReading> result)
    {
        hardware.Update();

        foreach (ISensor sensor in hardware.Sensors)
        {
            if (!TryMapSensorType(sensor.SensorType, out string category, out string unit))
            {
                continue;
            }

            if (!sensor.Value.HasValue)
            {
                continue;
            }

            result.Add(new SensorReading
            {
                HardwareName = hardware.Name,
                HardwareType = hardware.HardwareType.ToString(),
                SensorName = sensor.Name,
                Category = category,
                Unit = unit,
                Value = sensor.Value.Value
            });
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            ReadHardwareSensors(subHardware, result);
        }
    }

    /// <summary>
    /// 将 LibreHardwareMonitor 的传感器类型映射成可显示的分类和单位。
    /// 当前只返回温度，其余类型先注释保留，不参与返回。
    /// </summary>
    private static bool TryMapSensorType(SensorType sensorType, out string category, out string unit)
    {
        category = string.Empty;
        unit = string.Empty;

        switch (sensorType)
        {
            // CPU 温度 / GPU 温度 / 主板温度 / 磁盘温度
            case SensorType.Temperature:
                category = "温度";
                unit = "°C";
                return true;

            // 风扇转速
            // case SensorType.Fan:
            //     category = "风扇";
            //     unit = "RPM";
            //     return true;

            // 电压
            // case SensorType.Voltage:
            //     category = "电压";
            //     unit = "V";
            //     return true;

            // 频率，一般是 CPU Core Clock / GPU Core Clock / Memory Clock
            // case SensorType.Clock:
            //     category = "频率";
            //     unit = "MHz";
            //     return true;

            // 负载，一般是 CPU Total / GPU Core / Core #1 等百分比
            // case SensorType.Load:
            //     category = "负载";
            //     unit = "%";
            //     return true;

            default:
                return false;
        }
    }
}

/// <summary>
/// 单条传感器数据。
/// 当前虽然只显示温度，但数据结构仍保留通用字段，方便后续恢复其他传感器类型。
/// </summary>
public sealed class SensorReading
{
    public string HardwareName { get; init; } = string.Empty;

    public string HardwareType { get; init; } = string.Empty;

    public string SensorName { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public float Value { get; init; }
}

/// <summary>
/// 递归更新硬件树。
/// LibreHardwareMonitor 需要先 Update，传感器值才会刷新。
/// </summary>
internal sealed class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor)
    {
    }

    public void VisitParameter(IParameter parameter)
    {
    }
}
