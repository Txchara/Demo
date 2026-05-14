namespace Tool.Core.SysTools;

/// <summary>
/// 单条进程限制规则配置。
/// 每个实例描述一个需要被限制的目标进程关键字及其行为开关。
/// </summary>
public sealed class StartItem
{
    /// <summary>
    /// 进程名关键字，匹配规则：进程名包含该字符串即命中，不区分大小写。
    /// 例如填 "SGuard" 可同时命中 SGuard64.exe 和 SGuardSvc64.exe。
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 是否限制 CPU 优先级为最低（Idle）。
    /// </summary>
    public bool LimitPriority { get; set; } = true;

    /// <summary>
    /// 是否将 CPU 亲和性限制为仅最后一个逻辑核心。
    /// </summary>
    public bool LimitAffinity { get; set; } = true;

    /// <summary>
    /// 是否启用此条规则。设为 false 时扫描器跳过该关键字。
    /// </summary>
    public bool Enabled { get; set; } = true;
}
