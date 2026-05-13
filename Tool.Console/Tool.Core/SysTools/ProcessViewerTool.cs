using System.Diagnostics;

namespace Tool.Core.SysTools;

/// <summary>
/// 进程查看工具。
/// </summary>
public sealed class ProcessViewerTool
{
    /// <summary>
    /// 获取当前系统中的所有进程信息。
    /// </summary>
    public List<ProcessInfo> GetProcesses()
    {
        var result = new List<ProcessInfo>();

        foreach (Process process in Process.GetProcesses())
        {
            try
            {
                int processId = 0;
                string processName = "未知进程";
                string mainWindowTitle = string.Empty;
                string priorityClass = "未知";
                double workingSetMb = 0;
                bool isResponding = false;
                string startTimeText = "-";
                bool canRead = true;
                string? remark = null;

                // 逐项读取属性，避免某一个字段失败后整条进程信息被丢掉。
                try
                {
                    processId = process.Id;
                }
                catch (Exception ex)
                {
                    canRead = false;
                    remark = $"读取进程 ID 失败：{ex.Message}";
                }

                try
                {
                    processName = process.ProcessName;
                }
                catch (Exception ex)
                {
                    canRead = false;
                    remark ??= $"读取进程名失败：{ex.Message}";
                }

                try
                {
                    mainWindowTitle = process.MainWindowTitle;
                }
                catch (Exception ex)
                {
                    canRead = false;
                    remark ??= $"读取窗口标题失败：{ex.Message}";
                }

                try
                {
                    priorityClass = GetPriorityText(process.PriorityClass);
                }
                catch (Exception ex)
                {
                    canRead = false;
                    remark ??= $"读取优先级失败：{ex.Message}";
                }

                try
                {
                    workingSetMb = Math.Round(process.WorkingSet64 / 1024d / 1024, 2);
                }
                catch (Exception ex)
                {
                    canRead = false;
                    remark ??= $"读取内存占用失败：{ex.Message}";
                }

                try
                {
                    isResponding = process.Responding;
                }
                catch (Exception ex)
                {
                    canRead = false;
                    remark ??= $"读取响应状态失败：{ex.Message}";
                }

                try
                {
                    startTimeText = process.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (Exception ex)
                {
                    canRead = false;
                    remark ??= $"读取启动时间失败：{ex.Message}";
                }

                result.Add(new ProcessInfo
                {
                    ProcessId = processId,
                    ProcessName = processName,
                    MainWindowTitle = mainWindowTitle,
                    PriorityClass = priorityClass,
                    WorkingSetMb = workingSetMb,
                    IsResponding = isResponding,
                    StartTimeText = startTimeText,
                    CanRead = canRead,
                    Remark = remark
                });
            }
            finally
            {
                process.Dispose();
            }
        }

        return result;
    }

    /// <summary>
    /// 将优先级枚举转换为中文文本。
    /// </summary>
    private static string GetPriorityText(ProcessPriorityClass priorityClass)
    {
        return priorityClass switch
        {
            ProcessPriorityClass.Idle => "空闲",
            ProcessPriorityClass.BelowNormal => "低于正常",
            ProcessPriorityClass.Normal => "正常",
            ProcessPriorityClass.AboveNormal => "高于正常",
            ProcessPriorityClass.High => "高",
            ProcessPriorityClass.RealTime => "实时",
            _ => priorityClass.ToString()
        };
    }
}

/// <summary>
/// 单个进程的信息对象。
/// </summary>
public sealed class ProcessInfo
{
    /// <summary>
    /// 进程 ID。
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// 进程名称。
    /// </summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>
    /// 主窗口标题。无窗口程序通常为空。
    /// </summary>
    public string MainWindowTitle { get; init; } = string.Empty;

    /// <summary>
    /// 当前优先级名称。
    /// </summary>
    public string PriorityClass { get; init; } = string.Empty;

    /// <summary>
    /// 当前工作集内存占用，单位 MB。
    /// </summary>
    public double WorkingSetMb { get; init; }

    /// <summary>
    /// 进程是否处于响应状态。
    /// </summary>
    public bool IsResponding { get; init; }

    /// <summary>
    /// 进程启动时间文本。
    /// </summary>
    public string StartTimeText { get; init; } = string.Empty;

    /// <summary>
    /// 是否成功读取到主要信息。
    /// </summary>
    public bool CanRead { get; init; }

    /// <summary>
    /// 读取失败时的备注信息。
    /// </summary>
    public string? Remark { get; init; }
}
