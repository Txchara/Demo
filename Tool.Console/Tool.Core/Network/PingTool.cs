using System.Net.NetworkInformation;

namespace Tool.Core.Network;

public sealed class PingTool
{
    public async Task<PingResult> PingAsync(string address, int timeout = 3000)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new PingResult
            {
                Address = string.Empty,
                Success = false,
                Status = "Invalid",
                ErrorMessage = "Address cannot be empty."
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
                Status = reply.Status.ToString(),
                IpAddress = reply.Address?.ToString()
            };
        }
        catch (PingException ex)
        {
            return new PingResult
            {
                Address = trimmedAddress,
                Success = false,
                Status = "PingException",
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            return new PingResult
            {
                Address = trimmedAddress,
                Success = false,
                Status = "Error",
                ErrorMessage = ex.Message
            };
        }
    }
}
