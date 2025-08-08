// Copyright 2020 Takuto Nakamura
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

using FormsTimer = System.Windows.Forms.Timer;
using Microsoft.Win32;
using RunCat365.Properties;
using System.Diagnostics;

namespace RunCat365
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Terminate RunCat365 if there's any existing instance.
            using var procMutex = new Mutex(true, "_RUNCAT_MUTEX", out var result);
            if (!result) return;

            try
            {
                ApplicationConfiguration.Initialize();
                Application.SetColorMode(SystemColorMode.System);
                Application.Run(new RunCat365ApplicationContext());
            }
            finally
            {
                procMutex?.ReleaseMutex();
            }
        }
    }

    public class RunCat365ApplicationContext : ApplicationContext
    {
        private const int FETCH_TIMER_DEFAULT_INTERVAL = 1000;
        private const int FETCH_COUNTER_SIZE = 5;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        private readonly CPURepository cpuRepository;
        private readonly MemoryRepository memoryRepository;
        private readonly StorageRepository storageRepository;
        private readonly ContextMenuManager contextMenuManager;
        private readonly FormsTimer fetchTimer;
        private readonly FormsTimer animateTimer;
        private Runner runner = Runner.Cat;
        private Theme manualTheme = Theme.System;
        private FPSMaxLimit fpsMaxLimit = FPSMaxLimit.FPS40;
        private int fetchCounter = 5;

        public RunCat365ApplicationContext()
        {
            UserSettings.Default.Reload();
            _ = Enum.TryParse(UserSettings.Default.Runner, out runner);
            _ = Enum.TryParse(UserSettings.Default.Theme, out manualTheme);
            _ = Enum.TryParse(UserSettings.Default.FPSMaxLimit, out fpsMaxLimit);

            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuRepository = new CPURepository();
            memoryRepository = new MemoryRepository();
            storageRepository = new StorageRepository();

            contextMenuManager = new ContextMenuManager(
                () => runner,
                r => runner = r,
                () => GetSystemTheme(),
                () => manualTheme,
                t => manualTheme = t,
                () => fpsMaxLimit,
                f => fpsMaxLimit = f,
                () => GetStartup(),
                s => ToggleStartUp(s),
                () => OpenRepository(),
                () => Exit()
            );

            animateTimer = new FormsTimer
            {
                Interval = ANIMATE_TIMER_DEFAULT_INTERVAL
            };
            animateTimer.Tick += new EventHandler(AnimationTick);
            animateTimer.Start();

            fetchTimer = new FormsTimer
            {
                Interval = FETCH_TIMER_DEFAULT_INTERVAL
            };
            fetchTimer.Tick += new EventHandler(FetchTick);
            fetchTimer.Start();

            ShowBalloonTip();
        }

        private static Theme GetSystemTheme()
        {
            var keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            if (rKey is null) return Theme.Light;
            var value = rKey.GetValue("SystemUsesLightTheme");
            rKey.Close();
            if (value is null) return Theme.Light;
            return (int)value == 0 ? Theme.Dark : Theme.Light;
        }

        private static bool GetStartup()
        {
            var keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            if (rKey is null) return false;
            var value = (rKey.GetValue(Application.ProductName) is not null);
            rKey.Close();
            return value;
        }

        private static bool ToggleStartUp(bool isChecked)
        {
            var productName = Application.ProductName;
            if (productName is null) return false;
            var keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName, true);
            if (rKey is null) return false;
            if (isChecked)
            {
                rKey.DeleteValue(productName, false);
            }
            else
            {
                var fileName = Environment.ProcessPath;
                if (fileName != null)
                {
                    rKey.SetValue(productName, fileName);
                }
            }
            rKey.Close();
            return true;
        }

        private void ShowBalloonTip()
        {
            if (UserSettings.Default.FirstLaunch)
            {
                contextMenuManager.ShowBalloonTip();
                UserSettings.Default.FirstLaunch = false;
                UserSettings.Default.Save();
            }
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                var systemTheme = GetSystemTheme();
                contextMenuManager.SetIcons(systemTheme, manualTheme, runner);
            }
        }

        private static void OpenRepository()
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "https://github.com/Kyome22/RunCat365.git",
                    UseShellExecute = true
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        private void Exit()
        {
            cpuRepository.Close();
            animateTimer.Stop();
            fetchTimer.Stop();
            contextMenuManager.HideNotifyIcon();
            Application.Exit();
        }

        private void OnApplicationExit(object? sender, EventArgs e)
        {
            UserSettings.Default.Runner = runner.ToString();
            UserSettings.Default.Theme = manualTheme.ToString();
            UserSettings.Default.FPSMaxLimit = fpsMaxLimit.ToString();
            UserSettings.Default.Save();
        }

        private void AnimationTick(object? sender, EventArgs e)
        {
            contextMenuManager.AdvanceFrame();
        }

        private void FetchSystemInfo(
            CPUInfo cpuInfo,
            MemoryInfo memoryInfo,
            List<StorageInfo> storageValue
        )
        {
            contextMenuManager.SetNotifyIconText(cpuInfo.GetDescription());

            var systemInfoValues = new List<string>();
            systemInfoValues.AddRange(cpuInfo.GenerateIndicator());
            systemInfoValues.AddRange(memoryInfo.GenerateIndicator());
            systemInfoValues.AddRange(storageValue.GenerateIndicator());
            contextMenuManager.SetSystemInfoMenuText(string.Join("\n", [.. systemInfoValues]));
        }

        private int CalculateInterval(float cpuTotalValue)
        {
            // Range of interval: 25-500 (ms) = 2-40 (fps)
            var speed = (float)Math.Max(1.0f, (cpuTotalValue / 5.0f) * fpsMaxLimit.GetRate());
            return (int)(500.0f / speed);
        }

        private void FetchTick(object? state, EventArgs e)
        {
            cpuRepository.Update();
            fetchCounter += 1;
            if (fetchCounter < FETCH_COUNTER_SIZE) return;
            fetchCounter = 0;

            var cpuInfo = cpuRepository.Get();
            var memoryInfo = memoryRepository.Get();
            var storageInfo = storageRepository.Get();
            FetchSystemInfo(cpuInfo, memoryInfo, storageInfo);

            animateTimer.Stop();
            animateTimer.Interval = CalculateInterval(cpuInfo.Total);
            animateTimer.Start();
        }
    }
}
