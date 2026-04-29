using Tool.Core.Network;

Console.Write("Please input address: ");
string? address = Console.ReadLine();

if (string.IsNullOrWhiteSpace(address))
{
    Console.WriteLine("Address is empty.");
    return;
}

var pingTool = new PingTool();
PingResult result = await pingTool.PingAsync(address);

Console.WriteLine();
Console.WriteLine($"Address       : {result.Address}");
Console.WriteLine($"Success       : {result.Success}");
Console.WriteLine($"Status        : {result.Status}");
Console.WriteLine($"IP Address    : {result.IpAddress ?? "-"}");
Console.WriteLine($"Roundtrip(ms) : {result.RoundtripTime}");

if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
{
    Console.WriteLine($"Error         : {result.ErrorMessage}");
}
