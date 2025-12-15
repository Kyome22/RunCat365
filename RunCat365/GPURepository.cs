using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RunCat365
{
    internal class GPURepository
    {
        private PerformanceCounter gpuCounter;
        private bool isGpuAvailable = true;
        private readonly List<float> gpuInfoList = new List<float>();
        private const int GPU_INFO_LIST_LIMIT_SIZE = 5;

        public bool IsAvailable => isGpuAvailable;

        public GPURepository()
        {
            try
            {
                // try to find a gpu engine counter (windows 10+)
                var category = new PerformanceCounterCategory("GPU Engine");
                var instanceNames = category.GetInstanceNames();
                
                // look for 3d engine usage
                var instance = instanceNames.FirstOrDefault(n => n.Contains("engtype_3D"));
                if (instance != null)
                {
                    gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                    _ = gpuCounter.NextValue(); // discard first value
                }
                else
                {
                    isGpuAvailable = false;
                }
            }
            catch
            {
                // gpu counters not available on this system
                isGpuAvailable = false;
            }
        }

        public void Update()
        {
            if (!isGpuAvailable || gpuCounter == null) return;
            
            try
            {
                var value = Math.Min(100, gpuCounter.NextValue());
                gpuInfoList.Add(value);
                if (GPU_INFO_LIST_LIMIT_SIZE < gpuInfoList.Count)
                {
                    gpuInfoList.RemoveAt(0);
                }
            }
            catch
            {
                isGpuAvailable = false;
            }
        }

        public GPUInfo Get()
        {
            if (!isGpuAvailable || gpuInfoList.Count == 0)
            {
                return new GPUInfo(0);
            }
            return new GPUInfo(gpuInfoList.Average());
        }

        public void Close()
        {
            gpuCounter?.Close();
        }
    }

    public record struct GPUInfo(float Utilization);

    internal static class GPUInfoExtension
    {
        internal static List<string> GenerateIndicator(this GPUInfo gpuInfo)
        {
            var resultLines = new List<string>
            {
                $"GPU: {gpuInfo.Utilization:f1}%"
            };
            return resultLines;
        }

        internal static string GetDescription(this GPUInfo gpuInfo)
        {
            return $"GPU: {gpuInfo.Utilization:f1}%";
        }
    }
}
