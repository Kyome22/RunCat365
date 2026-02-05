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
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using FormsTimer = System.Windows.Forms.Timer;

namespace RunCat365
{
    internal class EndlessGameForm : Form
    {
        private const int JUMP_THREDHOLD = 17;
        private readonly FormsTimer timer;
        private readonly Theme systemTheme;
        private GameStatus status = GameStatus.NewGame;
        private Cat cat = new Cat.Running(Cat.Running.Frame.Frame0);
        private readonly List<Road> roads = [];
        private readonly Dictionary<string, Bitmap> catImages = [];
        private readonly Dictionary<Road, Bitmap> roadImages = [];
        private int counter = 0;
        private int limit = 5;
        private int score = 0;
        private int highScore = UserSettings.Default.HighScore;
        private bool isJumpRequested = false;
        private readonly bool isAutoPlay = false;
        private readonly Random random = new();
        private readonly Font scoreFont = new("Consolas", 15);
        private readonly Font messageFont = new("Segoe UI", 16, FontStyle.Bold);
        private readonly StringFormat rightAlignFormat = new()
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };
        private readonly StringFormat centerAlignFormat = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        private readonly SolidBrush overlayBrush = new(Color.FromArgb(77, 0, 0, 0));
        private SolidBrush textBrush = null!;

        internal EndlessGameForm(Theme systemTheme)
        {
            this.systemTheme = systemTheme;

            DoubleBuffered = true;
            ClientSize = new Size(600, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = Strings.Window_EndlessGame;
            Icon = Resources.AppIcon;
            BackColor = systemTheme == Theme.Light ? Color.Gainsboro : Color.Gray;

            var rm = Resources.ResourceManager;
            var rs = rm.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            if (rs is not null)
            {
                var catRegex = new Regex(@"^cat_.*_.*$");
                var color = systemTheme.GetContrastColor();
                foreach (DictionaryEntry entry in rs)
                {
                    var key = entry.Key.ToString();
                    if (string.IsNullOrEmpty(key)) continue;

                    if (catRegex.IsMatch(key))
                    {
                        if (entry.Value is Bitmap icon)
                        {
                            catImages.Add(key, systemTheme == Theme.Light ? new Bitmap(icon) : icon.Recolor(color));
                        }
                    }
                    else if (key.StartsWith("road_"))
                    {
                        if (entry.Value is Bitmap icon)
                        {
                            var roadType = key switch
                            {
                                "road_flat" => Road.Flat,
                                "road_hill" => Road.Hill,
                                "road_crater" => Road.Crater,
                                "road_sprout" => Road.Sprout,
                                _ => (Road?)null
                            };
                            if (roadType.HasValue)
                            {
                                roadImages.Add(roadType.Value, systemTheme == Theme.Light ? new Bitmap(icon) : icon.Recolor(color));
                            }
                        }
                    }
                }
            }

            textBrush = new SolidBrush(systemTheme.GetContrastColor());

            PrepareBitmaps();

            Paint += RenderScene;

            KeyDown += HandleKeyDown;

            timer = new FormsTimer
            {
                Interval = 100
            };
            timer.Tick += GameTick;

            Initialize();

            timer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            timer.Stop();
            timer.Dispose();

            foreach (var bitmap in catImages.Values) bitmap.Dispose();
            foreach (var bitmap in roadImages.Values) bitmap.Dispose();
            catImages.Clear();
            roadImages.Clear();

            scoreFont.Dispose();
            messageFont.Dispose();
            rightAlignFormat.Dispose();
            centerAlignFormat.Dispose();
            textBrush.Dispose();
            overlayBrush.Dispose();
        }

        private void PrepareBitmaps()
        {
            using var offscreen = new Bitmap(1, 1);
            using var g = Graphics.FromImage(offscreen);
            foreach (var bitmap in catImages.Values)
            {
                g.DrawImage(bitmap, 0, 0, 1, 1);
            }
            foreach (var bitmap in roadImages.Values)
            {
                g.DrawImage(bitmap, 0, 0, 1, 1);
            }
        }

        private void Initialize()
        {
            counter = JUMP_THREDHOLD;
            isJumpRequested = false;
            score = 0;
            cat = new Cat.Running(Cat.Running.Frame.Frame0);
            roads.RemoveAll(r => r == Road.Sprout);
            var roadsToAdd = 20 - roads.Count;
            for (int i = 0; i < roadsToAdd; i++)
            {
                roads.Add((Road)random.Next(0, 3));
            }
        }

        private bool Judge()
        {
            if (status != GameStatus.Playing) return false;
            foreach (var violationIndex in cat.ViolationIndices())
            {
                if (violationIndex < roads.Count && roads[violationIndex] == Road.Sprout)
                {
                    status = GameStatus.GameOver;
                    return false;
                }
            }
            return true;
        }

        private void UpdateRoads()
        {
            var firstRoad = roads.First();
            roads.RemoveAt(0);
            if (firstRoad == Road.Sprout)
            {
                score += 1;
                highScore = Math.Max(score, highScore);
            }
            counter = counter > 0 ? counter - 1 : limit - 1;
            if (counter == 0)
            {
                var randomValue = random.Next(0, 27);
                var subRoads = new List<Road>();
                if (randomValue % 3 == 0)
                {
                    subRoads.Add(Road.Sprout);
                }
                if (randomValue % 9 == 0)
                {
                    subRoads.Add(Road.Sprout);
                }
                if (randomValue % 27 == 0)
                {
                    subRoads.Add(Road.Sprout);
                }
                roads.AddRange(subRoads);
                limit = subRoads.Count == 0 ? 5 : 10;
            }
            if (roads.Count < 20)
            {
                roads.Add((Road)random.Next(0, 3));
            }
        }

        private void UpdateCat()
        {
            if (cat is Cat.Running runningCat)
            {
                if (runningCat.CurrentFrame == Cat.Running.Frame.Frame4 && isJumpRequested)
                {
                    cat = new Cat.Jumping(Cat.Jumping.Frame.Frame0);
                    isJumpRequested = false;
                    return;
                }
            }
            else if (cat is Cat.Jumping jumpingCat)
            {
                if (jumpingCat.CurrentFrame == Cat.Jumping.Frame.Frame9)
                {
                    if (isJumpRequested)
                    {
                        cat = cat.Next();
                        isJumpRequested = false;
                        return;
                    }
                    else
                    {
                        cat = new Cat.Running(Cat.Running.Frame.Frame0);
                        return;
                    }
                }
            }
            cat = cat.Next();
        }

        private void AutoJump()
        {
            if (isAutoPlay && roads[JUMP_THREDHOLD - 1] == Road.Sprout)
            {
                isJumpRequested = true;
            }
        }

        private void GameTick(object? sender, EventArgs e)
        {
            if (Judge())
            {
                UpdateRoads();
                UpdateCat();
                AutoJump();
            }
            Invalidate();
        }

        private void HandleKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                switch (status)
                {
                    case GameStatus.NewGame:
                    case GameStatus.GameOver:
                        Initialize();
                        status = GameStatus.Playing;
                        break;
                    case GameStatus.Playing when !isAutoPlay:
                        isJumpRequested = true;
                        break;
                    default:
                        break;
                }
            }
        }

        private void RenderScene(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.DrawString($"{Strings.Game_HighScore}: {highScore}", scoreFont, textBrush, new Rectangle(20, 0, 560, 50), rightAlignFormat);
            g.DrawString($"{Strings.Game_Score}: {score}", scoreFont, textBrush, new Rectangle(20, 30, 560, 50), rightAlignFormat);

            var roadCount = Math.Min(20, roads.Count);
            for (int i = 0; i < roadCount; i++)
            {
                if (roadImages.TryGetValue(roads[i], out var roadImage))
                {
                    g.DrawImage(roadImage, new Rectangle(i * 30, 200, 30, 50));
                }
            }

            var catKey = $"cat_{cat.GetString()}";
            if (catImages.TryGetValue(catKey, out var catImage))
            {
                g.DrawImage(catImage, new Rectangle(120, 130, 120, 100));
            }

            if (status != GameStatus.Playing)
            {
                g.FillRectangle(overlayBrush, new Rectangle(0, 0, 600, 250));

                var message = Strings.Game_PressSpaceToPlay;
                if (status == GameStatus.GameOver)
                {
                    if (score >= highScore)
                    {
                        SaveRecord(score);
                        message = $"{Strings.Game_NewRecord}!!\n{message}";
                    }
                    else
                    {
                        message = $"{Strings.Game_GameOver}\n{message}";
                    }
                }
                g.DrawString(message, messageFont, textBrush, new Rectangle(0, 0, 600, 250), centerAlignFormat);
            }
        }
        private void SaveRecord(int score)
        {
            UserSettings.Default.HighScore = score;
            UserSettings.Default.Save();
        }
    }
}