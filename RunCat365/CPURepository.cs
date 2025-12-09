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

namespace RunCat365;

internal struct CPUInfo
    {
        internal float Total { get; set; }
        internal float User { get; set; }
        internal float Kernel { get; set; }
        internal float Idle { get; set; }
    }

    internal static class CPUInfoExtension
    {
        internal static string GetDescription(this CPUInfo cpuInfo)
        {
            return $"CPU: {cpuInfo.Total:f1}%";
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
            return resultLines;
        }
    }

    internal class CPURepository
    {
        private readonly PerformanceCounter? totalCounter;
        private readonly PerformanceCounter? userCounter;
        private readonly PerformanceCounter? kernelCounter;
        private readonly PerformanceCounter? idleCounter;
        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;

        public CPURepository()
        {
            try
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
            catch
            {
                // Counters not available
            }
        }

        internal void Update()
        {
            // Range of value: 0-100 (%)
            float total = 0, user = 0, kernel = 0, idle = 0;

            if (totalCounter != null)
            {
                try
                {
                    total = Math.Min(100, totalCounter.NextValue());
                    user = Math.Min(100, userCounter!.NextValue());
                    kernel = Math.Min(100, kernelCounter!.NextValue());
                    idle = Math.Min(100, idleCounter!.NextValue());
                }
                catch { }
            }

            var cpuInfo = new CPUInfo
            {
                Total = total,
                User = user,
                Kernel = kernel,
                Idle = idle,
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
                Idle = cpuInfoList.Average(x => x.Idle)
            };
        }

        internal void Close()
        {
            totalCounter?.Close();
            userCounter?.Close();
            kernelCounter?.Close();
            idleCounter?.Close();
        }
    }

