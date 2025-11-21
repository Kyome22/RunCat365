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

using System.Runtime.InteropServices;
using System.Globalization;
using RunCat365.Properties;

namespace RunCat365
{
    struct MemoryInfo
    {
        internal uint MemoryLoad { get; set; }
        internal long TotalMemory { get; set; }
        internal long AvailableMemory { get; set; }
        internal long UsedMemory { get; set; }
    }

    internal static class MemoryInfoExtension
    {
        internal static List<string> GenerateIndicator(this MemoryInfo memoryInfo)
        {
            var memoryLabel = Resources.ResourceManager.GetString("Info.Memory", CultureInfo.CurrentUICulture) ?? "Memory: ";
            var totalLabel = Resources.ResourceManager.GetString("Info.Total", CultureInfo.CurrentUICulture) ?? "Total: ";
            var usedLabel = Resources.ResourceManager.GetString("Info.Used", CultureInfo.CurrentUICulture) ?? "Used: ";
            var availableLabel = Resources.ResourceManager.GetString("Info.Available", CultureInfo.CurrentUICulture) ?? "Available: ";

            var resultLines = new List<string>
            {
                $"{memoryLabel}{memoryInfo.MemoryLoad}%",
                $"   ├─ {totalLabel}{memoryInfo.TotalMemory.ToByteFormatted()}",
                $"   ├─ {usedLabel}{memoryInfo.UsedMemory.ToByteFormatted()}",
                $"   └─ {availableLabel}{memoryInfo.AvailableMemory.ToByteFormatted()}"
            };
            return resultLines;
        }
    }

    internal partial class MemoryRepository
    {
        private MemoryInfo memoryInfo;

        internal MemoryRepository()
        {
            memoryInfo = new MemoryInfo();
        }

        internal void Update()
        {
            var memStatus = new MemoryStatusEx();
            memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);

            if (GlobalMemoryStatusEx(ref memStatus))
            {
                memoryInfo.MemoryLoad = memStatus.dwMemoryLoad;
                memoryInfo.TotalMemory = (long)memStatus.ullTotalPhys;
                memoryInfo.AvailableMemory = (long)memStatus.ullAvailPhys;
                memoryInfo.UsedMemory = (long)(memStatus.ullTotalPhys - memStatus.ullAvailPhys);
            }
        }

        internal MemoryInfo Get()
        {
            Update();
            return memoryInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
    }
}
