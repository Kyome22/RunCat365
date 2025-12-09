// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using LibreHardwareMonitor.Hardware;

namespace RunCat365;

internal struct GPUInfo
{
    internal float Utilization { get; set; }
}

internal static class GPUInfoExtension
{
    internal static string GetCreativeMessage(float utilization)
    {
        return utilization switch
        {
            > 90 => " (MELTING!)",
            > 70 => " (Toasty)",
            > 50 => " (Warm)",
            > 20 => " (Purring)",
            _ => " (Chilling)"
        };
    }

    internal static List<string> GenerateIndicator(this GPUInfo gpuInfo)
    {
        var msg = GetCreativeMessage(gpuInfo.Utilization);
        return
        [
            $"GPU: {gpuInfo.Utilization:f1}%{msg}"
        ];
    }
}

internal class GPURepository
{
    private readonly Computer? computer;
    private readonly List<GPUInfo> gpuInfoList = [];
    private const int GPU_INFO_LIST_LIMIT_SIZE = 5;

    public GPURepository()
    {
        try
        {
            computer = new Computer
            {
                IsGpuEnabled = true
            };
            computer.Open();
        }
        catch
        {
            // May fail if not admin or dependencies missing
            computer = null;
        }
    }

    internal void Update()
    {
        float utilization = 0;
        if (computer != null)
        {
            try
            {
                foreach (var hardware in computer.Hardware)
                {
                    hardware.Update();
                    if (hardware.HardwareType == HardwareType.GpuNvidia || 
                        hardware.HardwareType == HardwareType.GpuAmd || 
                        hardware.HardwareType == HardwareType.GpuIntel)
                    {
                        foreach (var sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Value.HasValue)
                            {
                                // Take the maximum load (e.g. Core load) found across all GPUs
                                utilization = Math.Max(utilization, sensor.Value.Value);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors reading hardware
            }
        }

        var gpuInfo = new GPUInfo
        {
            Utilization = utilization
        };

        gpuInfoList.Add(gpuInfo);
        if (GPU_INFO_LIST_LIMIT_SIZE < gpuInfoList.Count)
        {
            gpuInfoList.RemoveAt(0);
        }
    }

    internal GPUInfo Get()
    {
        if (gpuInfoList.Count == 0) return new GPUInfo();

        return new GPUInfo
        {
            Utilization = gpuInfoList.Average(x => x.Utilization)
        };
    }

    internal void Close()
    {
        computer?.Close();
    }
}
