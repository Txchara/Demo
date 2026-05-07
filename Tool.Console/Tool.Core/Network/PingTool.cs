using System.Net.NetworkInformation;

namespace Tool.Core.Network;

/// <summary>
/// 提供基础的 Ping 探测能力，并将结果整理为统一返回对象。
/// </summary>
public sealed class PingTool
{
    /// <summary>
    /// 对指定地址执行 Ping，并返回尽量中文化的结果说明。
    /// </summary>
    public async Task<PingResult> PingAsync(string address, int timeout = 3000)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new PingResult
            {
                Address = string.Empty,
                Success = false,
                Status = "参数无效",
                ErrorMessage = "地址不能为空。"
            };
        }

        string trimmedAddress = address.Trim();

        try
        {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(trimmedAddress, timeout);

            return new PingResult
            {
                Address = trimmedAddress,
                Success = reply.Status == IPStatus.Success,
                RoundtripTime = reply.RoundtripTime,
                Status = GetStatusText(reply.Status),
                IpAddress = reply.Address?.ToString()
            };
        }
        catch (PingException ex)
        {
            return new PingResult
            {
                Address = trimmedAddress,
                Success = false,
                Status = "Ping 失败",
                ErrorMessage = GetPingExceptionMessage(ex)
            };
        }
        catch (Exception ex)
        {
            return new PingResult
            {
                Address = trimmedAddress,
                Success = false,
                Status = "执行异常",
                ErrorMessage = $"执行 Ping 时发生异常：{ex.Message}"
            };
        }
    }

    /// <summary>
    /// 将系统 IPStatus 转换为更直观的中文描述。
    /// </summary>
    private static string GetStatusText(IPStatus status)
    {
        return status switch
        {
            IPStatus.Success => "成功",
            IPStatus.TimedOut => "超时",
            IPStatus.DestinationHostUnreachable => "目标主机不可达",
            IPStatus.DestinationNetworkUnreachable => "目标网络不可达",
            IPStatus.DestinationPortUnreachable => "目标端口不可达",
            IPStatus.DestinationProtocolUnreachable => "目标协议不可达",
            IPStatus.BadRoute => "路由错误",
            IPStatus.PacketTooBig => "数据包过大",
            IPStatus.TtlExpired => "生存时间已到期",
            IPStatus.TtlReassemblyTimeExceeded => "数据包重组超时",
            IPStatus.ParameterProblem => "参数错误",
            IPStatus.SourceQuench => "源端被抑制",
            IPStatus.BadDestination => "目标地址错误",
            IPStatus.DestinationUnreachable => "目标不可达",
            IPStatus.TimeExceeded => "响应时间超限",
            IPStatus.BadHeader => "报文头错误",
            IPStatus.UnrecognizedNextHeader => "无法识别的下一报文头",
            IPStatus.IcmpError => "ICMP 错误",
            IPStatus.Unknown => "未知状态",
            _ => $"未知状态：{status}"
        };
    }

    /// <summary>
    /// 统一整理 PingException，避免直接把英文枚举或原始异常暴露出去。
    /// </summary>
    private static string GetPingExceptionMessage(PingException ex)
    {
        if (ex.InnerException is not null)
        {
            return $"Ping 执行失败：{ex.InnerException.Message}";
        }

        return $"Ping 执行失败：{ex.Message}";
    }
}
