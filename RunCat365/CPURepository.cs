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

using System.Diagnostics;
using System.Security.Principal;
using LHM = LibreHardwareMonitor.Hardware;

namespace RunCat365
{
    struct CPUInfo
    {
        internal float Total { get; set; }
        internal float User { get; set; }
        internal float Kernel { get; set; }
        internal float Idle { get; set; }
        internal float Temperature { get; set; }
    }

    internal static class CPUInfoExtension
    {
        internal static string GetDescription(this CPUInfo cpuInfo)
        {
            // If the temperature is 0, do not display ‘Temp’
            if (cpuInfo.Temperature <= 0)
                return $"CPU: {cpuInfo.Total:f1}%";

            return $"CPU: {cpuInfo.Total:f1}% | Temp: {cpuInfo.Temperature:f1}°C";
        }
        internal static List<string> GenerateIndicator(this CPUInfo cpuInfo)
        {
            var resultLines = new List<string>
            {
                $"CPU: {cpuInfo.Total:f1}%",
                $"   ├─ User: {cpuInfo.User:f1}%",
                $"   ├─ Kernel: {cpuInfo.Kernel:f1}%",
                $"   └─ Available: {cpuInfo.Idle:f1}%"
            };

            if (cpuInfo.Temperature > 0)
                resultLines.Add($"   └─ Temp: {cpuInfo.Temperature:f1}°C");

            return resultLines;
        }
    }

    internal class CPURepository
    {
        private readonly PerformanceCounter totalCounter;
        private readonly PerformanceCounter userCounter;
        private readonly PerformanceCounter kernelCounter;
        private readonly PerformanceCounter idleCounter;
        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;

        // Added additional support for motherboard and GPU (helps expose CPU sensors on some systems)
        private static readonly LHM.Computer computer = new LHM.Computer
        {
            IsCpuEnabled = true,
            IsMotherboardEnabled = true,
            IsGpuEnabled = true
        };

        static CPURepository()
        {
            computer.Open();
        }

        internal CPURepository()
        {
            totalCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            userCounter = new PerformanceCounter("Processor", "% User Time", "_Total");
            kernelCounter = new PerformanceCounter("Processor", "% Privileged Time", "_Total");
            idleCounter = new PerformanceCounter("Processor", "% Idle Time", "_Total");

            // Discards first return value
            _ = totalCounter.NextValue();
            _ = userCounter.NextValue();
            _ = kernelCounter.NextValue();
            _ = idleCounter.NextValue();
        }

        internal void Update()
        {
            // Range of value: 0-100 (%)
            var total = Math.Min(100, totalCounter.NextValue());
            var user = Math.Min(100, userCounter.NextValue());
            var kernel = Math.Min(100, kernelCounter.NextValue());
            var idle = Math.Min(100, idleCounter.NextValue());
            var temperature = GetCPUTemperature();

            var cpuInfo = new CPUInfo
            {
                Total = total,
                User = user,
                Kernel = kernel,
                Idle = idle,
                Temperature = temperature,
            };

            cpuInfoList.Add(cpuInfo);
            if (CPU_INFO_LIST_LIMIT_SIZE < cpuInfoList.Count)
            {
                cpuInfoList.RemoveAt(0);
            }
        }

        internal CPUInfo Get()
        {
            if (cpuInfoList.Count == 0) return new CPUInfo();

            return new CPUInfo
            {
                Total = cpuInfoList.Average(x => x.Total),
                User = cpuInfoList.Average(x => x.User),
                Kernel = cpuInfoList.Average(x => x.Kernel),
                Idle = cpuInfoList.Average(x => x.Idle),
                Temperature = cpuInfoList.Average(x => x.Temperature)
            };
        }

        // Only works if run as administrator
        public static float GetCPUTemperature()
        {
            if (!IsRunningAsAdministrator())
                return 0;

            if (computer == null)
                return 0;

            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == LHM.HardwareType.Cpu)
                {
                    // Updates hardware and sub-hardware
                    hardware.Update();
                    foreach (var subHardware in hardware.SubHardware)
                        subHardware.Update();

                    // Collects all sensors (CPU + subcomponents)
                    var sensors = hardware.Sensors.Concat(hardware.SubHardware.SelectMany(h => h.Sensors));

                    var temps = new List<float>();
                    foreach (var sensor in sensors)
                    {
                        // Filter by temperature sensors and check for null
                        if (sensor.SensorType == LHM.SensorType.Temperature && sensor.Value != null)
                        {
                            // Get the CPU temperature
                            if (sensor.Name.ToLower().Contains("package"))
                                return Convert.ToSingle(sensor.Value);

                            temps.Add(Convert.ToSingle(sensor.Value));
                        }
                    }

                    if (temps.Count > 0)
                    {
                        return temps.Average();
                    }
                }
            }

            return 0;
        }

        private static bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        internal void Close()
        {
            totalCounter.Close();
            userCounter.Close();
            kernelCounter.Close();
            idleCounter.Close();
        }
    }
}
