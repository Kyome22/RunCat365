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

namespace RunCat365;

internal enum Drive
    {
        C,
        D
    }

    internal static class DriveExtension
    {
        internal static string GetString(this Drive drive)
        {
            return drive switch
            {
                Drive.C => "C Drive",
                Drive.D => "D Drive",
                _ => "",
            };
        }

        internal static Drive? CreateFromString(string? value)
        {
            return value switch
            {
                "C:\\" => Drive.C,
                "D:\\" => Drive.D,
                _ => null,
            };
        }
    }

    struct StorageInfo
    {
        internal Drive Drive { get; set; }
        internal long TotalSize { get; set; }
        internal long AvailableSpaceSize { get; set; }
        internal long UsedSpaceSize { get; set; }
    }

    internal static class StorageInfoExtension
    {
        internal static List<string> GenerateIndicator(this List<StorageInfo> storageInfoList)
        {
            var resultLines = new List<string>
            {
                "Storage:"
            };

            if (storageInfoList.Count == 0) return resultLines;

            for (int i = 0; i < storageInfoList.Count; i++)
            {
                var info = storageInfoList[i];
                var isLastItem = (i == storageInfoList.Count - 1);
                var parentPrefix = isLastItem ? "   └─ " : "   ├─ ";
                var childIndent = isLastItem ? "      " : "   │  ";
                var percentage = ((double)info.UsedSpaceSize / info.TotalSize) * 100.0;
                resultLines.Add($"{parentPrefix}{info.Drive.GetString()}: {percentage:f1}%");
                resultLines.Add($"{childIndent}   ├─ Used: {info.UsedSpaceSize.ToByteFormatted()}");
                resultLines.Add($"{childIndent}   └─ Available: {info.AvailableSpaceSize.ToByteFormatted()}");
            }

            return resultLines;
        }
    }

    internal class StorageRepository
    {
        private readonly List<StorageInfo> storageInfoList = [];

        public StorageRepository() { }

        internal void Update()
        {
            storageInfoList.Clear();
            var allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo driveInfo in allDrives)
            {
                if (driveInfo.IsReady && DriveExtension.CreateFromString(driveInfo.Name) is Drive drive)
                {
                    try
                    {
                        var storageInfo = new StorageInfo
                        {
                            Drive = drive,
                            TotalSize = driveInfo.TotalSize,
                            AvailableSpaceSize = driveInfo.AvailableFreeSpace,
                            UsedSpaceSize = driveInfo.TotalSize - driveInfo.AvailableFreeSpace
                        };
                        storageInfoList.Add(storageInfo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
        }

        internal List<StorageInfo> Get()
        {
            Update();
            return storageInfoList;
        }
    }

