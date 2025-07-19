namespace NetworkActivityMonitor
{
    public class Connection
    {
        public string? AppName { get; set; }
        public int ProcessId { get; set; }
        public int LocalPort { get; set; }
        public string? RemoteAddress { get; set; }
        public string? RemoteHostName { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public string? Explanation { get; set; }
    }
}
