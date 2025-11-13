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
        private readonly Dictionary<int, long> prevCpuUse = new();

        public TopProcess Get()
        {
            Process[] processes = Process.GetProcesses();

            string topCpuProcessName = "UnknowUsen";
            long maxCpuUse = 0;

            string topRamProcessName = "UnknowUsen";
            long maxRamUse = 0;

            foreach (var p in processes)
            {
                try
                {
                    long nowUse = p.TotalProcessorTime.Ticks;
                    prevCpuUse.TryGetValue(p.Id, out long prevUse);
                    long diff = nowUse - prevUse;
                    prevCpuUse[p.Id] = nowUse;
                    if (diff > maxCpuUse)
                    {
                        maxCpuUse = diff;
                        topCpuProcessName = p.ProcessName;
                    }

                    long ram = p.WorkingSet64;
                    if (ram > maxRamUse)
                    {
                        maxRamUse = ram;
                        topRamProcessName = p.ProcessName;
                    }
                }
                catch
                {}
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
        public string TopCpuProcess { get; set; } = "UnknowUsen";
        public string TopRamProcess { get; set; } = "UnknowUsen";
    }

    
}
