using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RunCat365
{
    internal class GPURepository
    {
        private Computer computer;
        private IHardware gpu;
        private bool isGpuAvailable = true;

        public GPURepository()
        {
            try
            {
                computer = new Computer
                {
                    IsGpuEnabled = true
                };
                computer.Open();

                gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia ||
                                                          h.HardwareType == HardwareType.GpuAmd ||
                                                          h.HardwareType == HardwareType.GpuIntel);
                
                if (gpu == null)
                {
                    isGpuAvailable = false;
                }
            }
            catch (Exception)
            {
                // disable gpu if it fails (e.g. no admin rights)
                isGpuAvailable = false;
                computer = null;
            }
        }

        public void Update()
        {
            if (!isGpuAvailable || gpu == null) return;
            try
            {
                gpu.Update();
            }
            catch
            {
                isGpuAvailable = false;
            }
        }

        public GPUInfo Get()
        {
            if (!isGpuAvailable || gpu == null)
            {
                return new GPUInfo(0);
            }

            try
            {
                var loadSensor = gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);
                float value = loadSensor?.Value ?? 0;
                return new GPUInfo(value);
            }
            catch
            {
                // something went wrong, just stop trying
                isGpuAvailable = false;
                return new GPUInfo(0);
            }
        }

        public void Close()
        {
            computer?.Close();
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
