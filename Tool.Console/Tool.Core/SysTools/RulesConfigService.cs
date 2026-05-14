using System.Text.Json;

namespace Tool.Core.SysTools;

/// <summary>
/// 负责 StartItem 规则列表的 JSON 持久化。
/// 配置文件路径基于当前 exe 所在目录，发布或迁移到其他机器后路径不变。
/// </summary>
public sealed class RulesConfigService
{
    /// <summary>
    /// 配置文件名，固定放在 exe 同级目录下。
    /// 发布目录示例：D:\Temp\MyDemo\Publish\Tool\rules.json
    /// </summary>
    private static readonly string ConfigFileName = "rules.json";

    /// <summary>
    /// 配置文件完整路径，基于当前进程 exe 所在目录计算。
    /// AppContext.BaseDirectory 在发布为单文件或普通目录时均指向 exe 所在目录。
    /// </summary>
    private static readonly string ConfigFilePath =
        Path.Combine(AppContext.BaseDirectory, ConfigFileName);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 默认规则列表，配置文件不存在时写入并返回。
    /// </summary>
    private static readonly List<StartItem> DefaultRules = new()
    {
        new StartItem { Keyword = "SGuard", LimitPriority = true, LimitAffinity = true, Enabled = true },
        new StartItem { Keyword = "ACE",    LimitPriority = true, LimitAffinity = true, Enabled = true },
    };

    /// <summary>
    /// 读取规则列表。
    /// 文件不存在时自动写入默认规则并返回；文件损坏时返回默认规则但不覆盖文件。
    /// </summary>
    public List<StartItem> Load()
    {
        if (!File.Exists(ConfigFilePath))
        {
            // 首次运行，写入默认规则供用户参考和修改。
            Save(DefaultRules);
            return new List<StartItem>(DefaultRules);
        }

        try
        {
            string json = File.ReadAllText(ConfigFilePath);
            List<StartItem>? rules = JsonSerializer.Deserialize<List<StartItem>>(json, JsonOptions);
            // 反序列化结果为 null 或空列表时回退到默认规则，避免服务空转。
            return rules is { Count: > 0 } ? rules : new List<StartItem>(DefaultRules);
        }
        catch
        {
            // JSON 格式损坏时不覆盖文件，返回默认规则，让服务继续运行。
            return new List<StartItem>(DefaultRules);
        }
    }

    /// <summary>
    /// 将规则列表持久化到 JSON 文件。
    /// </summary>
    public void Save(List<StartItem> rules)
    {
        if (rules is null) throw new ArgumentNullException(nameof(rules));

        string json = JsonSerializer.Serialize(rules, JsonOptions);
        File.WriteAllText(ConfigFilePath, json);
    }

    /// <summary>
    /// 返回配置文件的完整路径，供 UI 层展示或调试使用。
    /// </summary>
    public static string GetConfigFilePath() => ConfigFilePath;
}
