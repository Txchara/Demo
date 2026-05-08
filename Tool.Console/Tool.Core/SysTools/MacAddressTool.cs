using System.Net.NetworkInformation;

namespace Tool.Core.SysTools;

/// <summary>
/// MAC 地址读取工具。
/// </summary>
public sealed class MacAddressTool
{
    /// <summary>
    /// 获取当前计算机可用网卡的 MAC 地址列表。
    /// </summary>
    public List<MacAddressInfo> GetMacAddresses()
    {
        var result = new List<MacAddressInfo>();

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

            PhysicalAddress physicalAddress = networkInterface.GetPhysicalAddress();
            byte[] bytes = physicalAddress.GetAddressBytes();

            if (bytes.Length == 0)
            {
                continue;
            }

            result.Add(new MacAddressInfo
            {
                Name = networkInterface.Name,
                Description = networkInterface.Description,
                InterfaceType = networkInterface.NetworkInterfaceType.ToString(),
                MacAddress = FormatMacAddress(bytes)
            });
        }

        return result;
    }

    private static string FormatMacAddress(byte[] bytes)
    {
        return string.Join(":", bytes.Select(b => b.ToString("X2")));
    }
}

/// <summary>
/// 单个网卡的 MAC 信息。
/// </summary>
public sealed class MacAddressInfo
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string InterfaceType { get; init; } = string.Empty;

    public string MacAddress { get; init; } = string.Empty;
}
