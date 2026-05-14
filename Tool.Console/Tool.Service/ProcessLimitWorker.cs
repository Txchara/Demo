using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tool.Core.SysTools;

namespace Tool.Service;

/// <summary>
/// 后台 Worker：定期扫描目标进程，发现后按 StartItem 配置执行限制。
/// 以 Windows Service 身份运行时无需用户登录，开机自启。
/// </summary>
public sealed class ProcessLimitWorker : BackgroundService
{
    /// <summary>
    /// 扫描间隔，每 5 秒检查一次是否有新的目标进程出现。
    /// </summary>
    private static readonly TimeSpan ScanInterval = TimeSpan.FromSeconds(5);

    private readonly ProcessViewerTool _tool = new();
    private readonly RulesConfigService _config = new();
    private readonly ILogger<ProcessLimitWorker> _logger;

    /// <summary>
    /// 记录已处理过的 PID，避免对同一个进程重复设置。
    /// 进程退出后其 PID 可能被系统复用，因此每轮扫描前会清理已退出的 PID。
    /// </summary>
    private readonly HashSet<int> _appliedPids = new();

    public ProcessLimitWorker(ILogger<ProcessLimitWorker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Worker 主循环，由宿主在后台线程调用，cancellationToken 触发时退出。
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProcessLimitWorker 已启动，扫描间隔 {Interval} 秒", ScanInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ScanAndLimit();
            }
            catch (Exception ex)
            {
                // 单轮扫描异常不中断整个服务，记录日志后继续下一轮。
                _logger.LogError(ex, "扫描进程时发生异常");
            }

            await Task.Delay(ScanInterval, stoppingToken);
        }

        _logger.LogInformation("ProcessLimitWorker 已停止");
    }

    /// <summary>
    /// 执行一轮扫描：
    /// 1. 清理 _appliedPids 中已退出的 PID，防止集合无限增长。
    /// 2. 遍历启用的规则，按关键字查找目标进程。
    /// 3. 对尚未处理过的进程按规则调用对应限制方法。
    /// </summary>
    private void ScanAndLimit()
    {
        CleanExitedPids();

        // 每轮重新读取配置，用户在 UI 保存后下一轮即生效，无需重启服务。
        List<StartItem> rules = _config.Load();

        foreach (StartItem rule in rules.Where(r => r.Enabled))
        {
            List<ProcessInfo> matched = _tool.FindProcessesByKeyword(rule.Keyword);

            foreach (ProcessInfo process in matched)
            {
                // 已处理过的 PID 跳过，避免每轮都重复设置。
                if (!_appliedPids.Add(process.ProcessId))
                {
                    continue;
                }

                _logger.LogInformation(
                    "发现目标进程：{Name}（PID: {Pid}），规则关键字：{Keyword}",
                    process.ProcessName, process.ProcessId, rule.Keyword);

                ApplyRule(rule, process.ProcessId);
            }
        }
    }

    /// <summary>
    /// 按规则的开关决定执行哪些限制操作。
    /// LimitPriority 和 LimitAffinity 均开启时复用 ApplyLimits 一次性完成；
    /// 只开启其中一项时单独调用对应方法，避免多余的系统调用。
    /// </summary>
    private void ApplyRule(StartItem rule, int processId)
    {
        if (rule.LimitPriority && rule.LimitAffinity)
        {
            ApplyLimitsResult result = _tool.ApplyLimits(processId);
            LogApplyLimitsResult(result);
            return;
        }

        if (rule.LimitPriority)
        {
            SetProcessPriorityResult result = _tool.SetLowestPriority(processId);
            LogPriorityResult(result);
        }

        if (rule.LimitAffinity)
        {
            SetAffinityResult result = _tool.SetAffinityToLastCore(processId);
            LogAffinityResult(result);
        }
    }

    /// <summary>
    /// 清理 _appliedPids 中已退出的进程 PID。
    /// 判断依据：GetProcessById 抛出 ArgumentException 说明进程已不存在。
    /// </summary>
    private void CleanExitedPids()
    {
        var exited = new List<int>();

        foreach (int pid in _appliedPids)
        {
            try
            {
                using var p = System.Diagnostics.Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                exited.Add(pid);
            }
        }

        foreach (int pid in exited)
        {
            _appliedPids.Remove(pid);
            _logger.LogInformation("进程 PID {Pid} 已退出，从已处理列表中移除", pid);
        }
    }

    private void LogApplyLimitsResult(ApplyLimitsResult result)
    {
        LogPriorityResult(result.PriorityResult);
        LogAffinityResult(result.AffinityResult);
    }

    private void LogPriorityResult(SetProcessPriorityResult result)
    {
        if (result.Success)
        {
            _logger.LogInformation(
                "{Name}（PID: {Pid}）优先级：{Old} → {New}",
                result.ProcessName, result.ProcessId,
                result.OldPriority, result.NewPriority);
        }
        else
        {
            _logger.LogWarning(
                "{Name}（PID: {Pid}）优先级设置失败：{Error}",
                result.ProcessName, result.ProcessId,
                result.ErrorMessage);
        }
    }

    private void LogAffinityResult(SetAffinityResult result)
    {
        if (result.Success)
        {
            _logger.LogInformation(
                "{Name}（PID: {Pid}）CPU 相关性：{Old} → {New}（共 {Count} 核）",
                result.ProcessName, result.ProcessId,
                result.OldAffinityText, result.NewAffinityText,
                result.CoreCount);
        }
        else
        {
            _logger.LogWarning(
                "{Name}（PID: {Pid}）CPU 相关性设置失败：{Error}",
                result.ProcessName, result.ProcessId,
                result.ErrorMessage);
        }
    }
}
