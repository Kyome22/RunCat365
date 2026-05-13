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
using System.ComponentModel;

namespace RunCat365
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> icons = [];
        private readonly Lock iconLock = new();
        private int current = 0;
        private EndlessGameForm? endlessGameForm;
        private CustomRunnerForm? customRunnerForm;

        internal ContextMenuManager(
            Func<Runner> getRunner,
            Action<Runner> setRunner,
            Func<Theme> getSystemTheme,
            Func<Theme> getManualTheme,
            Action<Theme> setManualTheme,
            Func<SpeedSource> getSpeedSource,
            Action<SpeedSource> setSpeedSource,
            Func<SpeedSource, bool> isSpeedSourceAvailable,
            Func<FPSMaxLimit> getFPSMaxLimit,
            Action<FPSMaxLimit> setFPSMaxLimit,
            Func<bool> getLaunchAtStartup,
            Func<bool, bool> toggleLaunchAtStartup,
            Action openRepository,
            Action onExit,
            CustomRunnerRepository customRunnerRepository,
            Func<string?> getCustomRunnerName,
            Action<string> applyCustomRunner,
            Action revertToBuiltInRunner
        )
        {
            systemInfoMenu.Text = "-\n-\n-\n-\n-";
            systemInfoMenu.Enabled = false;

            var runnersMenu = new CustomToolStripMenuItem(Strings.Menu_Runner);
            runnersMenu.SetupSubMenusFromEnum<Runner>(
                r => r.GetLocalizedString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Runner>(
                        parent,
                        sender,
                        (string? s, out Runner r) => Enum.TryParse(s, out r),
                        r => setRunner(r)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                r => getCustomRunnerName() is null && getRunner() == r,
                r => GetRunnerThumbnailBitmap(getSystemTheme(), r)
            );
            runnersMenu.DropDownOpening += (sender, e) => RefreshCustomRunnerMenu(
                runnersMenu,
                customRunnerRepository,
                getRunner,
                getCustomRunnerName,
                applyCustomRunner
            );

            var themeMenu = new CustomToolStripMenuItem(Strings.Menu_Theme);
            themeMenu.SetupSubMenusFromEnum<Theme>(
                t => t.GetLocalizedString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<Theme>(
                        parent,
                        sender,
                        (string? s, out Theme t) => Enum.TryParse(s, out t),
                        t => setManualTheme(t)
                    );
                    SetIcons(getSystemTheme(), getManualTheme(), getRunner());
                },
                t => getManualTheme() == t,
                _ => null
            );

            var speedSourceMenu = new CustomToolStripMenuItem(Strings.Menu_SpeedSource);
            speedSourceMenu.SetupSubMenusFromEnum<SpeedSource>(
                s => s.GetLocalizedString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<SpeedSource>(
                        parent,
                        sender,
                        (string? s, out SpeedSource ss) => Enum.TryParse(s, out ss),
                        s => setSpeedSource(s)
                    );
                },
                s => getSpeedSource() == s,
                _ => null,
                isSpeedSourceAvailable
            );

            var fpsMaxLimitMenu = new CustomToolStripMenuItem(Strings.Menu_FPSMaxLimit);
            fpsMaxLimitMenu.SetupSubMenusFromEnum<FPSMaxLimit>(
                f => f.GetString(),
                (parent, sender, e) =>
                {
                    HandleMenuItemSelection<FPSMaxLimit>(
                        parent,
                        sender,
                        (string? s, out FPSMaxLimit f) => FPSMaxLimitExtension.TryParse(s, out f),
                        f => setFPSMaxLimit(f)
                    );
                },
                f => getFPSMaxLimit() == f,
                _ => null
            );

            var launchAtStartupMenu = new CustomToolStripMenuItem(Strings.Menu_LaunchAtStartup)
            {
                Checked = getLaunchAtStartup()
            };
            launchAtStartupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleLaunchAtStartup);

            var settingsMenu = new CustomToolStripMenuItem(Strings.Menu_Settings);
            settingsMenu.DropDownItems.AddRange(
                themeMenu,
                speedSourceMenu,
                fpsMaxLimitMenu,
                launchAtStartupMenu
            );

            var customRunnersMenu = new CustomToolStripMenuItem(Strings.Menu_CustomRunners);
            customRunnersMenu.Click += (sender, e) => ShowOrActivateCustomRunnerWindow(
                customRunnerRepository, revertToBuiltInRunner
            );

            var endlessGameMenu = new CustomToolStripMenuItem(Strings.Menu_EndlessGame);
            endlessGameMenu.Click += (sender, e) => ShowOrActivateGameWindow(getSystemTheme);

            var appVersionMenu = new CustomToolStripMenuItem(
                $"{Application.ProductName} v{Application.ProductVersion}"
            )
            {
                Enabled = false
            };

            var repositoryMenu = new CustomToolStripMenuItem(Strings.Menu_OpenRepository);
            repositoryMenu.Click += (sender, e) => openRepository();

            var informationMenu = new CustomToolStripMenuItem(Strings.Menu_Information);
            informationMenu.DropDownItems.AddRange(
                appVersionMenu,
                repositoryMenu
            );

            var exitMenu = new CustomToolStripMenuItem(Strings.Menu_Exit);
            exitMenu.Click += (sender, e) => onExit();

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                systemInfoMenu,
                new ToolStripSeparator(),
                runnersMenu,
                customRunnersMenu,
                new ToolStripSeparator(),
                settingsMenu,
                informationMenu,
                endlessGameMenu,
                new ToolStripSeparator(),
                exitMenu
            );
            contextMenuStrip.Renderer = new ContextMenuRenderer();

            SetIcons(getSystemTheme(), getManualTheme(), getRunner());

            notifyIcon.Visible = true;
            notifyIcon.ContextMenuStrip = contextMenuStrip;
        }

        private static void HandleMenuItemSelection<T>(
            ToolStripMenuItem parentMenu,
            object? sender,
            CustomTryParseDelegate<T> tryParseMethod,
            Action<T> assignValueAction
        )
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem childItem in parentMenu.DropDownItems)
            {
                childItem.Checked = false;
            }
            item.Checked = true;

            if (item.Tag is T tagValue)
            {
                assignValueAction(tagValue);
            }
            else if (tryParseMethod(item.Text, out T parsedValue))
            {
                assignValueAction(parsedValue);
            }
        }

        private static Bitmap? GetRunnerThumbnailBitmap(Theme systemTheme, Runner runner)
        {
            var color = systemTheme.GetContrastColor();
            var iconName = $"{runner.GetString()}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            if (obj is not Bitmap bitmap) return null;
            return systemTheme == Theme.Light ? bitmap : bitmap.Recolor(color);
        }

        private static void RefreshCustomRunnerMenu(
            CustomToolStripMenuItem runnersMenu,
            CustomRunnerRepository customRunnerRepository,
            Func<Runner> getRunner,
            Func<string?> getCustomRunnerName,
            Action<string> applyCustomRunner
        )
        {
            foreach (ToolStripItem item in runnersMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem && item.Tag is Runner runner)
                {
                    menuItem.Checked = getCustomRunnerName() is null && getRunner() == runner;
                }
            }

            var customItems = runnersMenu.DropDownItems
                .Cast<ToolStripItem>()
                .Where(item => item.Tag is CustomRunnerMenuTag)
                .ToList();
            foreach (var item in customItems)
            {
                runnersMenu.DropDownItems.Remove(item);
                item.Dispose();
            }

            var profiles = customRunnerRepository.GetAll().OrderBy(p => p.Name).ToList();
            if (profiles.Count == 0) return;

            runnersMenu.DropDownItems.Add(new ToolStripSeparator { Tag = new CustomRunnerMenuTag("") });
            foreach (var profile in profiles)
            {
                var name = profile.Name;
                var item = new CustomToolStripMenuItem(name)
                {
                    Tag = new CustomRunnerMenuTag(name),
                    Checked = string.Equals(getCustomRunnerName(), name, StringComparison.OrdinalIgnoreCase)
                };
                item.Click += (sender, e) => applyCustomRunner(name);
                runnersMenu.DropDownItems.Add(item);
            }
        }

        internal void SetIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var theme = manualTheme == Theme.System ? systemTheme : manualTheme;
            var color = theme.GetContrastColor();
            var runnerName = runner.GetString();
            var rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{runnerName}_{i}".ToLower();
                if (rm.GetObject(iconName) is not Bitmap bitmap) continue;
                if (theme == Theme.Light)
                {
                    list.Add(bitmap.ToIcon());
                }
                else
                {
                    using var recolored = bitmap.Recolor(color);
                    list.Add(recolored.ToIcon());
                }
            }

            lock (iconLock)
            {
                icons.Clear();
                icons.AddRange(list);
                current = 0;
            }
        }

        internal void SetCustomIcons(List<Bitmap> frames, Theme systemTheme, Theme manualTheme)
        {
            var theme = manualTheme == Theme.System ? systemTheme : manualTheme;
            var color = theme.GetContrastColor();
            var list = new List<Icon>(frames.Count);
            foreach (var frame in frames)
            {
                if (theme == Theme.Light)
                {
                    list.Add(frame.ToIcon());
                }
                else
                {
                    using var recolored = frame.Recolor(color);
                    list.Add(recolored.ToIcon());
                }
            }

            lock (iconLock)
            {
                icons.Clear();
                icons.AddRange(list);
                current = 0;
            }
        }

        private static void HandleStartupMenuClick(object? sender, Func<bool, bool> toggleLaunchAtStartup)
        {
            if (sender is null) return;
            var item = (ToolStripMenuItem)sender;
            try
            {
                if (toggleLaunchAtStartup(item.Checked))
                {
                    item.Checked = !item.Checked;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, Strings.Message_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        private void ShowOrActivateGameWindow(Func<Theme> getSystemTheme)
        {
            if (endlessGameForm is null)
            {
                endlessGameForm = new EndlessGameForm(getSystemTheme());
                endlessGameForm.FormClosed += (sender, e) =>
                {
                    endlessGameForm = null;
                };
                endlessGameForm.Show();
            }
            else
            {
                endlessGameForm.Activate();
            }
        }

        private void ShowOrActivateCustomRunnerWindow(
            CustomRunnerRepository repository,
            Action revertToBuiltInRunner
        )
        {
            if (customRunnerForm is null)
            {
                customRunnerForm = new CustomRunnerForm(repository, revertToBuiltInRunner);
                customRunnerForm.FormClosed += (sender, e) =>
                {
                    customRunnerForm = null;
                };
                customRunnerForm.Show();
            }
            else
            {
                customRunnerForm.Activate();
            }
        }

        internal void ShowBalloonTip(BalloonTipType balloonTipType)
        {
            var info = balloonTipType.GetInfo();
            notifyIcon.ShowBalloonTip(5000, info.Title, info.Text, info.Icon);
        }

        internal void AdvanceFrame()
        {
            lock (iconLock)
            {
                if (icons.Count == 0) return;
                if (icons.Count <= current) current = 0;
                notifyIcon.Icon = icons[current];
                current = (current + 1) % icons.Count;
            }
        }

        internal void SetSystemInfoMenuText(string text)
        {
            systemInfoMenu.Text = text;
        }

        internal void SetNotifyIconText(string text)
        {
            notifyIcon.Text = text;
        }

        internal void HideNotifyIcon()
        {
            notifyIcon.Visible = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (iconLock)
                {
                    icons.Clear();
                }

                if (notifyIcon is not null)
                {
                    notifyIcon.ContextMenuStrip?.Dispose();
                    notifyIcon.Dispose();
                }

                endlessGameForm?.Dispose();
                customRunnerForm?.Dispose();
            }
        }

        private delegate bool CustomTryParseDelegate<T>(string? value, out T result);

        private sealed record CustomRunnerMenuTag(string Name);
    }
}
