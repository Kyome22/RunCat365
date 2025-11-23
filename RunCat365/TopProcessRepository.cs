using System.Diagnostics;

namespace RunCat365
{
    internal static class TopProcessExtension
    {
        internal static List<string> GenerateIndicator(this TopProcess tp)
        {
            return new List<string>
            {
                "Top Process:",
                $"   ├─ CPU: {tp.TopCpuProcess}",
                $"   └─ RAM: {tp.TopRamProcess}"
            };
        }
    }

    internal class TopProcessRepository
    {
        private readonly Dictionary<int, long> previousCpuUse = new();

        public TopProcess Get()
        {
            var processes = Process.GetProcesses();

            var topCpuProcessName = "unknown";
            long maxCpuUse = 0;

            var topRamProcessName = "unknown";
            long maxRamUse = 0;

            foreach (var p in processes)
            {
                try
                {
                    var currentCpuUsage = p.TotalProcessorTime.Ticks;
                    previousCpuUse.TryGetValue(p.Id, out var previousCpuUsage);

                    var cpuUsageDiff = currentCpuUsage - previousCpuUsage;
                    previousCpuUse[p.Id] = currentCpuUsage;

                    if (cpuUsageDiff > maxCpuUse)
                    {
                        maxCpuUse = cpuUsageDiff;
                        topCpuProcessName = p.ProcessName;
                    }

                    var ram = p.WorkingSet64;
                    if (ram > maxRamUse)
                    {
                        maxRamUse = ram;
                        topRamProcessName = p.ProcessName;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to read process info {p.Id}: {ex.Message}");
                }
            }

            return new TopProcess
            {
                TopCpuProcess = topCpuProcessName,
                TopRamProcess = topRamProcessName
            };
        }
    }

    internal class TopProcess
    {
        public string TopCpuProcess { get; set; } = "unknown";
        public string TopRamProcess { get; set; } = "unknown";
    }
}
