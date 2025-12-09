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

            var runnersMenu = new CustomToolStripMenuItem("Runners");
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

            var themeMenu = new CustomToolStripMenuItem("Theme");
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

            var fpsMaxLimitMenu = new CustomToolStripMenuItem("FPS Max Limit");
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

            var launchAtStartupMenu = new CustomToolStripMenuItem("Launch at startup")
            {
                Checked = getLaunchAtStartup()
            };
            launchAtStartupMenu.Click += (sender, e) => HandleStartupMenuClick(sender, toggleLaunchAtStartup);

            var settingsMenu = new CustomToolStripMenuItem("Settings");
            settingsMenu.DropDownItems.AddRange(
                themeMenu,
                fpsMaxLimitMenu,
                launchAtStartupMenu
            );

            var endlessGameMenu = new CustomToolStripMenuItem("Endless Game");
            endlessGameMenu.Click += (sender, e) => ShowOrActivateGameWindow(getSystemTheme);

            var appVersionMenu = new CustomToolStripMenuItem(
                $"{Application.ProductName} v{Application.ProductVersion}"
            )
            {
                Enabled = false
            };

            var repositoryMenu = new CustomToolStripMenuItem("Open Repository");
            repositoryMenu.Click += (sender, e) => openRepository();

            var informationMenu = new CustomToolStripMenuItem("Information");
            informationMenu.DropDownItems.AddRange(
                appVersionMenu,
                repositoryMenu
            );

            var exitMenu = new CustomToolStripMenuItem("Exit");
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
            notifyIcon.Icon = icons[0];
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
            var iconName = $"{systemTheme.GetString()}_{runner.GetString()}_0".ToLower();
            var obj = Resources.ResourceManager.GetObject(iconName);
            return obj is Icon icon ? icon.ToBitmap() : null;
        }

        internal void SetIcons(Theme systemTheme, Theme manualTheme, Runner runner)
        {
            var prefix = (manualTheme == Theme.System ? systemTheme : manualTheme).GetString();
            var runnerName = runner.GetString();
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
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            var message = "App has launched. " +
                "If the icon is not on the taskbar, it has been omitted, " +
                "so please move it manually and pin it.";
            notifyIcon.ShowBalloonTip(5000, "RunCat 365", message, ToolTipIcon.Info);
        }

        internal void AdvanceFrame(float gpuLoad = 0)
        {
            lock (iconLock)
            {
                if (icons.Count == 0) return;
                if (icons.Count <= current) current = 0;
                
                var baseIcon = icons[current];
                
                if (gpuLoad > 0)
                {
                    // Tint based on GPU Load (Heat: 0% Normal -> 100% Red)
                    try 
                    {
                        var tintedIcon = ApplyHeatTint(baseIcon, gpuLoad);
                        notifyIcon.Icon = tintedIcon;
                        
                        // Clean up the PREVIOUS manually created icon to avoid GDI leaks.
                        // Note: We do NOT dispose baseIcon as it is in the 'icons' cache list.
                        if (currentDisplayedIcon != null && currentDisplayedIcon != baseIcon)
                        {
                            currentDisplayedIcon.Dispose();
                        }
                        currentDisplayedIcon = tintedIcon;
                    }
                    catch 
                    {
                        // Fallback if GDI fails
                        notifyIcon.Icon = baseIcon;
                    }
                }
                else
                {
                    notifyIcon.Icon = baseIcon;
                    if (currentDisplayedIcon != null && currentDisplayedIcon != baseIcon)
                    {
                        currentDisplayedIcon.Dispose();
                        currentDisplayedIcon = null;
                    }
                }

                current = (current + 1) % icons.Count;
            }
        }

        private Icon ApplyHeatTint(Icon source, float load)
        {
            // Calculate tint intensity (0.0 to 1.0)
            float intensity = Math.Clamp(load / 100f, 0f, 1f);
            
            // Start showing color only after 10% load to keep clean look at idle
            if (intensity < 0.1f) return (Icon)source.Clone();

            using var bitmap = source.ToBitmap();
            
            // Heat Color (Red/Orange)
            var heatColor = Color.FromArgb(255, 255, 69, 0); // OrangeRed

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.A > 0)
                    {
                        // Simple tint: Interpolate between original pixel and heat color
                        var r = (int)(pixel.R + (heatColor.R - pixel.R) * intensity);
                        var g = (int)(pixel.G + (heatColor.G - pixel.G) * intensity);
                        var b = (int)(pixel.B + (heatColor.B - pixel.B) * intensity);
                        
                        bitmap.SetPixel(x, y, Color.FromArgb(pixel.A, r, g, b));
                    }
                }
            }
            
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private Icon? currentDisplayedIcon;


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
