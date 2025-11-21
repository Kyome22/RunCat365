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
using System.Globalization;

namespace RunCat365
{
    internal class ContextMenuManager : IDisposable
    {
        private readonly CustomToolStripMenuItem systemInfoMenu = new();
        private readonly NotifyIcon notifyIcon = new();
        private readonly List<Icon> icons = [];
        private readonly object iconLock = new();
        private int current = 0;
        private EndlessGameForm? endlessGameForm;

        internal ContextMenuManager(
            Func<Runner> getRunner,
            Action<Runner> setRunner,
            Func<Theme> getSystemTheme,
            Func<Theme> getManualTheme,
            Action<Theme> setManualTheme,
            Func<FPSMaxLimit> getFPSMaxLimit,
            Action<FPSMaxLimit> setFPSMaxLimit,
            Func<bool> getLaunchAtStartup,
            Func<bool, bool> toggleLaunchAtStartup,
            Action openRepository,
            Action onExit
        )
        {
            systemInfoMenu.Text = "-\n-\n-\n-\n-";
            systemInfoMenu.Enabled = false;

            var runnersMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.Runners", CultureInfo.CurrentUICulture) ?? "Runners"
            );
            runnersMenu.SetupSubMenusFromEnum<Runner>(
                r => r.GetString(),
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
                r => getRunner() == r,
                r => GetRunnerThumbnailBitmap(getSystemTheme(), r)
            );

            var themeMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.Theme", CultureInfo.CurrentUICulture) ?? "Theme"
            );
            themeMenu.SetupSubMenusFromEnum<Theme>(
                t => t.GetString(),
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

            var fpsMaxLimitMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.FPSMaxLimit", CultureInfo.CurrentUICulture) ?? "FPS Max Limit"
            );
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

            var launchAtStartupMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.LaunchAtStartup", CultureInfo.CurrentUICulture) ?? "Launch at startup"
            )
            {
                Checked = getLaunchAtStartup()
            };
            launchAtStartupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleLaunchAtStartup);
            
            var settingsMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.Settings", CultureInfo.CurrentUICulture) ?? "Settings"
            );
            settingsMenu.DropDownItems.AddRange(
                themeMenu,
                fpsMaxLimitMenu,
                launchAtStartupMenu
            );

            var endlessGameMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.EndlessGame", CultureInfo.CurrentUICulture) ?? "Endless Game"
            );
            endlessGameMenu.Click += (sender, e) => ShowOrActivateGameWindow(getSystemTheme);
            
            var appVersionMenu = new CustomToolStripMenuItem(
                $"{Application.ProductName} v{Application.ProductVersion}"
            )
            {
                Enabled = false
            };

            var repositoryMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.OpenRepository", CultureInfo.CurrentUICulture) ?? "Open Repository"
            );
            repositoryMenu.Click += (sender, e) => openRepository();

            var informationMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.Information", CultureInfo.CurrentUICulture) ?? "Information"
            );
            informationMenu.DropDownItems.AddRange(
                appVersionMenu,
                repositoryMenu
            );

            var exitMenu = new CustomToolStripMenuItem(
                Resources.ResourceManager.GetString("Menu.Exit", CultureInfo.CurrentUICulture) ?? "Exit"
            );
            exitMenu.Click += (sender, e) => onExit();

            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(
                systemInfoMenu,
                new ToolStripSeparator(),
                runnersMenu,
                new ToolStripSeparator(),
                settingsMenu,
                informationMenu,
                endlessGameMenu,
                new ToolStripSeparator(),
                exitMenu
            );
            contextMenuStrip.Renderer = new ContextMenuRenderer();

            SetIcons(getSystemTheme(), getManualTheme(), getRunner());

            notifyIcon.Text = "-";
            if (icons.Count > 0) 
            {
                notifyIcon.Icon = icons[0];
            }
            else
            {
                notifyIcon.Icon = Resources.AppIcon;
            }

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
            if (tryParseMethod(item.Text, out T parsedValue))
            {
                assignValueAction(parsedValue);
            }
        }

        private static Bitmap? GetRunnerThumbnailBitmap(Theme systemTheme, Runner runner)
        {
            var iconName = $"{systemTheme.GetResourceName()}_{runner.GetResourceName()}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            return obj is Icon icon ? icon.ToBitmap() : null;
        }
        internal void SetIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var prefix = (manualTheme == Theme.System ? systemTheme : manualTheme).GetResourceName();
            var runnerName = runner.GetResourceName();
            var rm = Resources.ResourceManager;
            var capacity = runner.GetFrameNumber();
            var list = new List<Icon>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                var iconName = $"{prefix}_{runnerName}_{i}".ToLower();
                var icon = rm.GetObject(iconName);
                if (icon is null) continue;
                list.Add((Icon)icon);
            }
            lock (iconLock)
            {
                icons.ForEach(icon => icon.Dispose());
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
                var warningTitle = Resources.ResourceManager.GetString("Error.Warning", CultureInfo.CurrentUICulture) ?? "Warning";
                MessageBox.Show(ex.Message, warningTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        internal void ShowBalloonTip()
        {
            var message = Resources.ResourceManager.GetString("Notify.Launched", CultureInfo.CurrentUICulture) ?? 
                          "App has launched. If the icon is not on the taskbar, it has been omitted, so please move it manually and pin it.";
            var title = Resources.ResourceManager.GetString("Notify.Title", CultureInfo.CurrentUICulture) ?? "RunCat 365";

            notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
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
                    icons.ForEach(icon => icon.Dispose());
                    icons.Clear();
                }

                if (notifyIcon is not null)
                {
                    notifyIcon.ContextMenuStrip?.Dispose();
                    notifyIcon.Dispose();
                }

                endlessGameForm?.Dispose();
            }
        }

        private delegate bool CustomTryParseDelegate<T>(string? value, out T result);
    }
}
