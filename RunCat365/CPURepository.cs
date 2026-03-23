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

using RunCat365.Properties;
using System.Runtime.InteropServices;

namespace RunCat365
{
    struct CPUInfo
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
            return $"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}%";
        }

        internal static List<string> GenerateIndicator(this CPUInfo cpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}%"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_User}: {cpuInfo.User:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Kernel}: {cpuInfo.Kernel:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Available}: {cpuInfo.Idle:f1}%", true)
            };
            return resultLines;
        }
    }

    internal class CPURepository
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(
            out long idleTime,
            out long kernelTime,
            out long userTime);

        private long prevIdleTime;
        private long prevKernelTime;
        private long prevUserTime;
        private bool hasPreviousSample;

        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;

        internal bool IsAvailable { get; }

        internal CPURepository()
        {
            IsAvailable = GetSystemTimes(out prevIdleTime, out prevKernelTime, out prevUserTime);
            hasPreviousSample = IsAvailable;
        }

        internal void Update()
        {
            if (!IsAvailable) return;

            if (!GetSystemTimes(out long idleTime, out long kernelTime, out long userTime))
                return;

            if (!hasPreviousSample)
            {
                prevIdleTime = idleTime;
                prevKernelTime = kernelTime;
                prevUserTime = userTime;
                hasPreviousSample = true;
                return;
            }

            long idleDelta = idleTime - prevIdleTime;
            long kernelDelta = kernelTime - prevKernelTime;
            long userDelta = userTime - prevUserTime;
            long totalDelta = kernelDelta + userDelta;

            prevIdleTime = idleTime;
            prevKernelTime = kernelTime;
            prevUserTime = userTime;

            if (totalDelta == 0) return;

            // kernelTime includes idle time, so actual kernel = kernelDelta - idleDelta
            float idlePercent = (float)idleDelta / totalDelta * 100f;
            float kernelPercent = (float)(kernelDelta - idleDelta) / totalDelta * 100f;
            float userPercent = (float)userDelta / totalDelta * 100f;
            float totalPercent = 100f - idlePercent;

            var cpuInfo = new CPUInfo
            {
                Total = Math.Clamp(totalPercent, 0f, 100f),
                User = Math.Clamp(userPercent, 0f, 100f),
                Kernel = Math.Clamp(kernelPercent, 0f, 100f),
                Idle = Math.Clamp(idlePercent, 0f, 100f),
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
            // No resources to release for GetSystemTimes
        }
    }
}
