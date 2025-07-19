using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System;
using System.Net;
using System.Threading.Tasks;
using MaxMind.GeoIP2;

namespace NetworkActivityMonitor
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Connection> Connections { get; set; }
        public string ConnectionSummary { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Connections = new ObservableCollection<Connection>();
            ConnectionSummary = "Connections: 0 total";
            DataContext = this;
            GetConnections();
        }

        private async void GetConnections()
        {
            Connections.Clear();
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            foreach (var c in connections)
            {
                try
                {
                    var processId = GetProcessIdForTcpConnection(c);
                    var processName = "N/A";
                    if (processId != -1)
                    {
                        try
                        {
                            var process = Process.GetProcessById(processId);
                            processName = process.ProcessName;
                        }
                        catch (System.Exception)
                        {
                            // Process might have exited
                        }
                    }

                    var remoteHostName = await GetHostName(c.RemoteEndPoint.Address);
                    var location = await GetLocation(c.RemoteEndPoint.Address);
                    var status = GetStatus(remoteHostName);
                    var explanation = GetExplanation(remoteHostName);

                    Connections.Add(new Connection
                    {
                        AppName = processName,
                        ProcessId = processId,
                        LocalPort = c.LocalEndPoint.Port,
                        RemoteAddress = c.RemoteEndPoint.Address.ToString(),
                        RemoteHostName = remoteHostName,
                        Location = location,
                        Status = status,
                        Explanation = explanation
                    });
                }
                catch (System.Exception)
                {
                    // Handle exceptions
                }
            }
            ConnectionSummary = $"Connections: {Connections.Count} total";
        }

        private async Task<string> GetHostName(IPAddress ipAddress)
        {
            try
            {
                IPHostEntry hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                return hostEntry.HostName;
            }
            catch (Exception)
            {
                return "N/A";
            }
        }

        private async Task<string> GetLocation(IPAddress ipAddress)
        {
            try
            {
                // TODO: Replace with your actual account ID and license key
                using (var client = new WebServiceClient(accountId: 0, licenseKey: "YOUR_LICENSE_KEY"))
                {
                    var city = await client.CityAsync(ipAddress);
                    return $"{city.Country.Name}, {city.City.Name}";
                }
            }
            catch (Exception)
            {
                return "N/A";
            }
        }

        private string GetStatus(string? hostName)
        {
            // TODO: Implement more sophisticated status detection
            if (hostName == null || hostName == "N/A")
            {
                return "Unknown";
            }
            if (hostName.Contains("google.com") || hostName.Contains("microsoft.com"))
            {
                return "Trusted";
            }
            return "Unknown";
        }

        private string GetExplanation(string? hostName)
        {
            // TODO: Implement more sophisticated explanation generation
            if (hostName == null || hostName == "N/A")
            {
                return "No information available.";
            }
            if (hostName.Contains("google.com"))
            {
                return "This is a connection to a Google service.";
            }
            if (hostName.Contains("microsoft.com"))
            {
                return "This is a connection to a Microsoft service.";
            }
            return "No information available.";
        }

        private int GetProcessIdForTcpConnection(TcpConnectionInformation connection)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var command = $"lsof -i TCP:{connection.LocalEndPoint.Port} -sTCP:ESTABLISHED -n -P -t";
                var output = RunBashCommand(command);
                if (int.TryParse(output, out int processId))
                {
                    return processId;
                }
            }
            else
            {
                // For Windows and other platforms, this needs to be implemented
            }
            return -1;
        }

        private string RunBashCommand(string command)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result.Trim();
        }
    }
}
