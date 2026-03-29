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
using RunCat365.Properties;

namespace RunCat365
{
    struct TemperatureInfo
    {
        internal float Current { get; set; }
    }

    internal static class TemperatureInfoExtension
    {
        internal static string GetDescription(this TemperatureInfo tempInfo)
        {
            return $"Temp: {(int)Math.Round(tempInfo.Current)}°C";
        }

        internal static List<string> GenerateIndicator(this TemperatureInfo tempInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_Temperature}:"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Temperature}: {tempInfo.Current:f1}°C", true)
            };
            return resultLines;
        }
    }

    internal class TemperatureRepository
    {
        private readonly List<PerformanceCounter> tempCounters = [];
        private readonly Queue<float> tempRingBuffer = new();
        private const int TEMP_BUFFER_LIMIT_SIZE = 60;

        internal bool IsAvailable { get; private set; } = true;

        internal TemperatureRepository()
        {
            try
            {
                var category = new PerformanceCounterCategory("Thermal Zone Information");
                var instanceNames = category.GetInstanceNames();
                if (instanceNames.Length > 0)
                {
                    foreach (var instance in instanceNames)
                    {
                        var counter = new PerformanceCounter("Thermal Zone Information", "Temperature", instance);
                        tempCounters.Add(counter);

                        // Discards first return value
                        _ = counter.NextValue();
                    }
                }
                else
                {
                    IsAvailable = false;
                }
            }
            catch
            {
                IsAvailable = false;
            }
        }

        internal void Update()
        {
            if (!IsAvailable || tempCounters.Count == 0) return;
            try
            {
                var values = tempCounters.Select(counter => counter.NextValue()).ToList();
                var currentKelvin = values.Count > 0 ? values.Max() : 0f;
                // Convert Kelvin to Celsius
                var currentCelsius = currentKelvin - 273.15f;

                tempRingBuffer.Enqueue(currentCelsius);
                if (TEMP_BUFFER_LIMIT_SIZE < tempRingBuffer.Count)
                {
                    tempRingBuffer.Dequeue();
                }
            }
            catch
            {
                IsAvailable = false;
            }
        }

        internal TemperatureInfo? Get()
        {
            if (!IsAvailable || tempRingBuffer.Count == 0) return null;

            return new TemperatureInfo
            {
                Current = tempRingBuffer.Last()
            };
        }

        internal float? GetLatestCelsius()
        {
            if (!IsAvailable || tempRingBuffer.Count == 0) return null;
            return tempRingBuffer.Last();
        }

        internal void Close()
        {
            foreach (var counter in tempCounters)
            {
                counter.Close();
            }
        }
    }
}
