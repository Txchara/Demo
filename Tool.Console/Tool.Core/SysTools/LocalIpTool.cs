using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Tool.Core.SysTools;

/// <summary>
/// 本机 IP 地址读取工具。
/// </summary>
public sealed class LocalIpTool
{
    /// <summary>
    /// 获取当前计算机可用网卡的本机 IPv4 地址列表。
    /// </summary>
    public List<LocalIpInfo> GetLocalIps()
    {
        var result = new List<LocalIpInfo>();

        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();

            foreach (UnicastIPAddressInformation unicastAddress in ipProperties.UnicastAddresses)
            {
                if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                if (IPAddress.IsLoopback(unicastAddress.Address))
                {
                    continue;
                }

                result.Add(new LocalIpInfo
                {
                    Name = networkInterface.Name,
                    Description = networkInterface.Description,
                    InterfaceType = networkInterface.NetworkInterfaceType.ToString(),
                    IpAddress = unicastAddress.Address.ToString()
                });
            }
        }

        return result;
    }
}

/// <summary>
/// 单个网卡的本机 IP 信息。
/// </summary>
public sealed class LocalIpInfo
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string InterfaceType { get; init; } = string.Empty;

    public string IpAddress { get; init; } = string.Empty;
}
