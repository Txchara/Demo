using System;
using System.Collections.Generic;
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
    /// 根据缓存里的进程 PID，将对应进程的优先级设置为最低。
    /// </summary>
    public SetProcessPriorityResult SetLowestPriority(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);

            string processName = process.ProcessName;
            string oldPriority = GetPriorityText(process.PriorityClass);

            process.PriorityClass = ProcessPriorityClass.Idle;

            return new SetProcessPriorityResult
            {
                Success = true,
                ProcessId = processId,
                ProcessName = processName,
                OldPriority = oldPriority,
                NewPriority = GetPriorityText(process.PriorityClass)
            };
        }
        catch (Exception ex)
        {
            return new SetProcessPriorityResult
            {
                Success = false,
                ProcessId = processId,
                ErrorMessage = ex.Message
            };
        }
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

    /// <summary>
    /// 将指定进程的 CPU 亲和性限制为仅最后一个逻辑核心。
    /// 原理：ProcessorAffinity 是位掩码，每一位对应一个逻辑核心（位为1表示允许运行）。
    /// 例如 8 核系统：全部核心 = 0xFF（11111111），只用最后一个核心 = 0x80（10000000）。
    /// 计算方式：1 左移 (核心数-1) 位，得到只有最高位为1的掩码。
    /// 需要对目标进程有 PROCESS_SET_INFORMATION 权限，通常要求以管理员身份运行。
    /// </summary>
    /// <param name="processId"></param>
    public SetAffinityResult SetAffinityToLastCore(int processId)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);
            string processName = process.ProcessName;
            // 读取当前亲和性掩码，转为二进制字符串便于展示(如 '11111111')
            long oldMask = process.ProcessorAffinity.ToInt64();
            string oldAffinityText = Convert.ToString(oldMask,2).PadLeft(Environment.ProcessorCount,'0');
            // 计算只保留最后一个核心的掩码
            // Environment.ProcessorCount 返回逻辑核心数 (含超线程)
            int coreCount = Environment.ProcessorCount;
            long newMask = 1L << (coreCount - 1);
            string newAffinityText = Convert.ToString(newMask, 2).PadLeft(coreCount, '0');
            process.ProcessorAffinity = new IntPtr(newMask);
            return new SetAffinityResult
            {
                Success = true,
                ProcessId = processId,
                ProcessName = processName,
                OldAffinityText  = oldAffinityText,
                NewAffinityText = newAffinityText,
                CoreCount = coreCount
            };
        }
        catch (Exception ex)
        {
            return new SetAffinityResult
            {
                Success = false,
                ProcessId = processId,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>                                                                                          
    /// 按关键字搜索进程名，返回匹配的进程列表。                                                                                                                                                                                                 /// 用于流程第一步：找到目标进程（如搜索"ACE"或"SG"）。
    /// 匹配规则：进程名包含关键字即命中，不区分大小写。
    /// </summary>
    public List<ProcessInfo> FindProcessesByKeyword(string keyword)
    {
        return GetProcesses()
            .Where(p => p.ProcessName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>                       
    /// 对指定进程同时执行优先级限制和 CPU 亲和性限制。    
    /// </summary>
    /// <param name="processId">目标进程的 PID，通常来自 FindProcessesByKeyword 的搜索结果。</param>
    public ApplyLimitsResult ApplyLimits(int processId)
    {
        //对应任务管理器流程：右键进程 → 设置优先级为最低 + 设置相关性为仅最后一个核心。
        //两个操作独立执行，一个失败不会中断另一个，调用方通过 FullSuccess 判断是否全部成功。      
        //需要以管理员身份运行，否则对受保护进程（如 SGuard64.exe）会抛出拒绝访问异常。

        // 优先级设置为 最低
        SetProcessPriorityResult priorityResult = SetLowestPriority(processId);

        // 将 CPU 亲和性限制为仅最后一个逻辑核心
        SetAffinityResult affinityResult = SetAffinityToLastCore(processId);

        // 优先从成功的一方取进程名；两者都失败时为空字符串，与各自结果对象保持一致
        string processName = priorityResult.Success
            ? priorityResult.ProcessName
            : affinityResult.ProcessName;

        return new ApplyLimitsResult
        {
            ProcessId = processId,
            ProcessName = processName,
            PriorityResult = priorityResult,
            AffinityResult = affinityResult
            // FullSuccess 由结果类自动计算：PriorityResult.Success && AffinityResult.Success
        };
    }
}
#region

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
/// <summary>
/// 设置进程优先级的结果。
/// </summary>
public sealed class SetProcessPriorityResult
{
    /// <summary>
    /// 是否设置成功。
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 目标进程 PID。
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// 目标进程名称。
    /// </summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>
    /// 设置前的优先级。
    /// </summary>
    public string OldPriority { get; init; } = string.Empty;

    /// <summary>
    /// 设置后的优先级。
    /// </summary>
    public string NewPriority { get; init; } = string.Empty;

    /// <summary>
    /// 失败时的错误信息。
    /// </summary>
    public string? ErrorMessage { get; init; }
}
/// <summary>
/// 设置 CPU 亲和性的结果。
/// </summary>
public sealed class SetAffinityResult
{
    /// <summary>是否设置成功。</summary>
    public bool Success { get; init; }

    /// <summary>目标进程 PID。</summary>
    public int ProcessId { get; init; }

    /// <summary>目标进程名称。</summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>设置前的亲和性掩码，二进制字符串，如 "11111111"。</summary>
    public string OldAffinityText { get; init; } = string.Empty;

    /// <summary>设置后的亲和性掩码，二进制字符串，如 "10000000"。</summary>
    public string NewAffinityText { get; init; } = string.Empty;

    /// <summary>系统逻辑核心总数，用于解释掩码位数。</summary>
    public int CoreCount { get; init; }

    /// <summary>失败时的错误信息。</summary>
    public string? ErrorMessage { get; init; }
}
/// <summary>
/// 对单个进程同时执行优先级 + 亲和性限制的组合结果。
/// 两个操作独立执行，一个失败不影响另一个。
/// </summary>
public sealed class ApplyLimitsResult
{
    /// <summary>目标进程 PID。</summary>
    public int ProcessId { get; init; }

    /// <summary>目标进程名称。</summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>优先级设置结果（写死为 Idle，复用现有 SetLowestPriority）。</summary>
    public SetProcessPriorityResult PriorityResult { get; init; } = null!;

    /// <summary>CPU 亲和性设置结果。</summary>
    public SetAffinityResult AffinityResult { get; init; } = null!;

    /// <summary>两项操作是否全部成功。</summary>
    public bool FullSuccess => PriorityResult.Success && AffinityResult.Success;
}

#endregion
