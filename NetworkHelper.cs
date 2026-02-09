using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public static class NetworkHelper
{
    public static string GetLanIpAddress()
    {
        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Only real usable adapters
            if (network.OperationalStatus != OperationalStatus.Up)
                continue;

            // Skip virtual + loopback adapters
            if (network.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                network.Description.ToLower().Contains("virtual") ||
                network.Description.ToLower().Contains("vmware") ||
                network.Description.ToLower().Contains("hyper-v"))
                continue;

            var properties = network.GetIPProperties();

            foreach (var addr in properties.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ip = addr.Address.ToString();

                    // We only want private LAN ranges
                    if (ip.StartsWith("192.168.") ||
                        ip.StartsWith("10.") ||
                        ip.StartsWith("172.16.") ||
                        ip.StartsWith("172.17.") ||
                        ip.StartsWith("172.18.") ||
                        ip.StartsWith("172.19.") ||
                        ip.StartsWith("172.20.") ||
                        ip.StartsWith("172.21.") ||
                        ip.StartsWith("172.22.") ||
                        ip.StartsWith("172.23.") ||
                        ip.StartsWith("172.24.") ||
                        ip.StartsWith("172.25.") ||
                        ip.StartsWith("172.26.") ||
                        ip.StartsWith("172.27.") ||
                        ip.StartsWith("172.28.") ||
                        ip.StartsWith("172.29.") ||
                        ip.StartsWith("172.30.") ||
                        ip.StartsWith("172.31."))
                    {
                        return ip;
                    }
                }
            }
        }

        return "localhost";
    }
}
