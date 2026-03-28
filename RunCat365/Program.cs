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

using Microsoft.Win32;
using RunCat365.Properties;
using System.Diagnostics;
using System.Globalization;
using FormsTimer = System.Windows.Forms.Timer;

namespace RunCat365
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
#if DEBUG
            var defaultCultureInfo = SupportedLanguage.English.GetDefaultCultureInfo();
#else
            var defaultCultureInfo = SupportedLanguageExtension.GetCurrentLanguage().GetDefaultCultureInfo();
#endif
            CultureInfo.CurrentUICulture = defaultCultureInfo;
            CultureInfo.CurrentCulture = defaultCultureInfo;

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

    internal class RunCat365ApplicationContext : ApplicationContext
    {
        private const int FETCH_TIMER_DEFAULT_INTERVAL = 1000;
        private const int FETCH_COUNTER_SIZE = 5;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;
        
        private readonly CPURepository cpuRepository;
        private readonly GPURepository gpuRepository;
        private readonly MemoryRepository memoryRepository;
        private readonly StorageRepository storageRepository;
        private readonly NetworkRepository networkRepository;
        private readonly LaunchAtStartupManager launchAtStartupManager;

        private FormsTimer? fetchTimer;
        private int fetchCounter = 5;

        private ContextMenuManager? cpuContextMenu;
        private ContextMenuManager? gpuContextMenu;
        private ContextMenuManager? memoryContextMenu;

        private FormsTimer? cpuAnimateTimer;
        private FormsTimer? gpuAnimateTimer;
        private FormsTimer? memoryAnimateTimer;

        private Theme manualTheme = Theme.System;
        private FPSMaxLimit fpsMaxLimit = FPSMaxLimit.FPS40;

        private bool showCpu = true;
        private Runner cpuRunner = Runner.Cat;
        private bool showGpu = false;
        private Runner gpuRunner = Runner.GamingCat;
        private bool showMemory = false;
        private Runner memoryRunner = Runner.PartyParrot;

        public RunCat365ApplicationContext()
        {
            UserSettings.Default.Reload();
            showCpu = UserSettings.Default.ShowCPU;
            _ = Enum.TryParse(UserSettings.Default.CPURunner, out cpuRunner);
            showGpu = UserSettings.Default.ShowGPU;
            _ = Enum.TryParse(UserSettings.Default.GPURunner, out gpuRunner);
            showMemory = UserSettings.Default.ShowMemory;
            _ = Enum.TryParse(UserSettings.Default.MemoryRunner, out memoryRunner);

            _ = Enum.TryParse(UserSettings.Default.Theme, out manualTheme);
            _ = Enum.TryParse(UserSettings.Default.FPSMaxLimit, out fpsMaxLimit);

            SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(UserPreferenceChanged);

            cpuRepository = new CPURepository();
            gpuRepository = new GPURepository();
            memoryRepository = new MemoryRepository();
            storageRepository = new StorageRepository();
            networkRepository = new NetworkRepository();
            launchAtStartupManager = new LaunchAtStartupManager();

            cpuContextMenu = CreateContextMenu(SpeedSource.CPU);
            gpuContextMenu = CreateContextMenu(SpeedSource.GPU);
            memoryContextMenu = CreateContextMenu(SpeedSource.Memory);

            cpuAnimateTimer = CreateAnimateTimer(() => cpuContextMenu?.AdvanceFrame());
            gpuAnimateTimer = CreateAnimateTimer(() => gpuContextMenu?.AdvanceFrame());
            memoryAnimateTimer = CreateAnimateTimer(() => memoryContextMenu?.AdvanceFrame());

            fetchTimer = new FormsTimer { Interval = FETCH_TIMER_DEFAULT_INTERVAL };
            fetchTimer.Tick += new EventHandler(FetchTick);
            fetchTimer.Start();

            ShowBalloonTipIfNeeded();
        }

        private ContextMenuManager CreateContextMenu(SpeedSource identity)
        {
            return new ContextMenuManager(
                identity,
                (src) => GetRunner(src),
                (src, r) => ChangeRunner(src, r),
                () => GetSystemTheme(),
                () => manualTheme,
                t => ChangeManualTheme(t),
                (src) => IsIconActive(src),
                (src, active) => ToggleIconActive(src, active),
                s => IsSpeedSourceAvailable(s),
                () => fpsMaxLimit,
                f => ChangeFPSMaxLimit(f),
                () => launchAtStartupManager.GetStartup(),
                s => launchAtStartupManager.SetStartup(s),
                () => OpenRepository(),
                () => Application.Exit()
            );
        }

        private FormsTimer CreateAnimateTimer(Action advanceFrame)
        {
            var timer = new FormsTimer { Interval = ANIMATE_TIMER_DEFAULT_INTERVAL };
            timer.Tick += (s, e) => advanceFrame();
            timer.Start();
            return timer;
        }

        private static Theme GetSystemTheme()
        {
            var keyName = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var rKey = Registry.CurrentUser.OpenSubKey(keyName);
            if (rKey is null) return Theme.Light;
            var value = rKey.GetValue("SystemUsesLightTheme");
            if (value is null) return Theme.Light;
            return (int)value == 0 ? Theme.Dark : Theme.Light;
        }

        private bool IsSpeedSourceAvailable(SpeedSource speedSource)
        {
            return speedSource switch
            {
                SpeedSource.CPU => true,
                SpeedSource.GPU => gpuRepository.IsAvailable,
                SpeedSource.Memory => true,
                _ => false,
            };
        }

        private void ShowBalloonTipIfNeeded()
        {
            if (!cpuRepository.IsAvailable)
            {
                cpuContextMenu?.ShowBalloonTip(BalloonTipType.CPUInfoUnavailable);
            }
            else if (UserSettings.Default.FirstLaunch)
            {
                cpuContextMenu?.ShowBalloonTip(BalloonTipType.AppLaunched);
                UserSettings.Default.FirstLaunch = false;
                UserSettings.Default.Save();
            }
        }

        private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                var systemTheme = GetSystemTheme();
                cpuContextMenu?.SetIcons(systemTheme, manualTheme, cpuRunner);
                gpuContextMenu?.SetIcons(systemTheme, manualTheme, gpuRunner);
                memoryContextMenu?.SetIcons(systemTheme, manualTheme, memoryRunner);
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

        private Runner GetRunner(SpeedSource src) => src switch
        {
            SpeedSource.CPU => cpuRunner,
            SpeedSource.GPU => gpuRunner,
            SpeedSource.Memory => memoryRunner,
            _ => Runner.Cat,
        };

        private void ChangeRunner(SpeedSource src, Runner r)
        {
            switch (src)
            {
                case SpeedSource.CPU:
                    cpuRunner = r;
                    UserSettings.Default.CPURunner = r.ToString();
                    break;
                case SpeedSource.GPU:
                    gpuRunner = r;
                    UserSettings.Default.GPURunner = r.ToString();
                    break;
                case SpeedSource.Memory:
                    memoryRunner = r;
                    UserSettings.Default.MemoryRunner = r.ToString();
                    break;
            }
            UserSettings.Default.Save();
        }

        private bool IsIconActive(SpeedSource src) => src switch
        {
            SpeedSource.CPU => showCpu,
            SpeedSource.GPU => showGpu,
            SpeedSource.Memory => showMemory,
            _ => false,
        };

        private void ToggleIconActive(SpeedSource src, bool active)
        {
            switch (src)
            {
                case SpeedSource.CPU:
                    showCpu = active;
                    UserSettings.Default.ShowCPU = active;
                    break;
                case SpeedSource.GPU:
                    showGpu = active;
                    UserSettings.Default.ShowGPU = active;
                    break;
                case SpeedSource.Memory:
                    showMemory = active;
                    UserSettings.Default.ShowMemory = active;
                    break;
            }
            if (!showCpu && !showGpu && !showMemory)
            {
                showCpu = true;
                UserSettings.Default.ShowCPU = true;
            }
            UserSettings.Default.Save();
            
            cpuContextMenu?.HideNotifyIcon();
            gpuContextMenu?.HideNotifyIcon();
            memoryContextMenu?.HideNotifyIcon();
            if (showCpu)
            {
                cpuContextMenu?.ShowNotifyIcon();
            }
            if (showGpu)
            {
                gpuContextMenu?.ShowNotifyIcon();
            }
            if (showMemory)
            {
                memoryContextMenu?.ShowNotifyIcon();
            }
        }

        private void ChangeManualTheme(Theme t)
        {
            manualTheme = t;
            UserSettings.Default.Theme = manualTheme.ToString();
            UserSettings.Default.Save();
            UserPreferenceChanged(this, new UserPreferenceChangedEventArgs(UserPreferenceCategory.General));
        }

        private void ChangeFPSMaxLimit(FPSMaxLimit f)
        {
            fpsMaxLimit = f;
            UserSettings.Default.FPSMaxLimit = fpsMaxLimit.ToString();
            UserSettings.Default.Save();
        }

        private int CalculateInterval(float load)
        {
            var speed = (float)Math.Max(1.0f, (load / 5.0f) * fpsMaxLimit.GetRate());
            return (int)(500.0f / speed);
        }

        private void FetchTick(object? state, EventArgs e)
        {
            cpuRepository.Update();
            gpuRepository.Update();
            fetchCounter += 1;
            if (fetchCounter < FETCH_COUNTER_SIZE) return;
            fetchCounter = 0;
            
            var cpuInfo = cpuRepository.Get();
            var gpuInfo = gpuRepository.Get();
            var memoryInfo = memoryRepository.Get();
            var storageInfo = storageRepository.Get();
            var networkInfo = networkRepository.Get();

            var sysInfoList = new List<string>();
            sysInfoList.AddRange(cpuInfo.GenerateIndicator());
            if (gpuInfo.HasValue) sysInfoList.AddRange(gpuInfo.Value.GenerateIndicator());
            sysInfoList.AddRange(memoryInfo.GenerateIndicator());
            sysInfoList.AddRange(storageInfo.GenerateIndicator());
            sysInfoList.AddRange(networkInfo.GenerateIndicator());
            var sysInfoText = string.Join("\n", sysInfoList);

            cpuContextMenu?.SetSystemInfoMenuText(sysInfoText);
            gpuContextMenu?.SetSystemInfoMenuText(sysInfoText);
            memoryContextMenu?.SetSystemInfoMenuText(sysInfoText);

            cpuContextMenu?.SetNotifyIconText(cpuInfo.GetDescription());
            if (gpuInfo.HasValue) gpuContextMenu?.SetNotifyIconText(gpuInfo.Value.GetDescription());
            memoryContextMenu?.SetNotifyIconText(memoryInfo.GetDescription());

            var cpuInterval = CalculateInterval(cpuInfo.Total);
            var gpuInterval = CalculateInterval(gpuInfo?.Maximum ?? 0f);
            var memInterval = CalculateInterval(memoryInfo.MemoryLoad);

            if (cpuAnimateTimer != null)
            {
                cpuAnimateTimer.Stop();
                cpuAnimateTimer.Interval = cpuInterval;
                cpuAnimateTimer.Start();
            }
            if (gpuAnimateTimer != null)
            {
                gpuAnimateTimer.Stop();
                gpuAnimateTimer.Interval = gpuInterval;
                gpuAnimateTimer.Start();
            }
            if (memoryAnimateTimer != null)
            {
                memoryAnimateTimer.Stop();
                memoryAnimateTimer.Interval = memInterval;
                memoryAnimateTimer.Start();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;

                cpuAnimateTimer?.Stop();
                cpuAnimateTimer?.Dispose();
                gpuAnimateTimer?.Stop();
                gpuAnimateTimer?.Dispose();
                memoryAnimateTimer?.Stop();
                memoryAnimateTimer?.Dispose();
                fetchTimer?.Stop();
                fetchTimer?.Dispose();

                cpuRepository?.Close();

                cpuContextMenu?.HideNotifyIcon();
                cpuContextMenu?.Dispose();
                gpuContextMenu?.HideNotifyIcon();
                gpuContextMenu?.Dispose();
                memoryContextMenu?.HideNotifyIcon();
                memoryContextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
