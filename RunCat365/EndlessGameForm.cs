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
        private int counter = 0;
        private int limit = 5;
        private int score = 0;
        private bool isJumpRequested = false;
        private readonly bool isAutoPlay = false;

        internal EndlessGameForm(Theme systemTheme)
        {
            this.systemTheme = systemTheme;

            DoubleBuffered = true;
            ClientSize = new Size(600, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Endless Game";
            Icon = Resources.AppIcon;
            BackColor = systemTheme == Theme.Light ? Color.Gainsboro : Color.Gray;

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
        }

        private void Initialize()
        {
            counter = JUMP_THREDHOLD;
            isJumpRequested = false;
            score = 0;
            cat = new Cat.Running(Cat.Running.Frame.Frame0);
            roads.RemoveAll(r => r == Road.Sprout);
            Enumerable.Range(0, 20 - roads.Count).ToList().ForEach(
                _ => roads.Add((Road)(new Random().Next(0, 3)))
            );
        }

        private bool Judge()
        {
            if (status != GameStatus.Playing) return false;
            var sproutIndices = roads
                .Select((road, index) => { return road == Road.Sprout ? index : (int?)null; })
                .OfType<int>()
                .ToList();
            if (cat.ViolationIndices().HasCommonElements(sproutIndices))
            {
                status = GameStatus.GameOver;
                return false;
            }
            else
            {
                return true;
            }
        }

        private void UpdateRoads()
        {
            var firstRoad = roads.First();
            roads.RemoveAt(0);
            if (firstRoad == Road.Sprout)
            {
                score += 1;
                if (score > UserSettings.Default.HighScore)
                {
                    UserSettings.Default.HighScore = score;
                }
            }
            counter = counter > 0 ? counter - 1 : limit - 1;
            if (counter == 0)
            {
                var randomValue = new Random().Next(0, 27);
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
                roads.Add((Road)(new Random().Next(0, 3)));
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
            var rm = Resources.ResourceManager;
            var prefix = systemTheme.GetString();
            var textColor = systemTheme == Theme.Light ? Color.Black : Color.White;
            var g = e.Graphics;

            using (Font font15 = new("Courier New", 15))
            using (Brush brush = new SolidBrush(textColor))
            {
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString($"Score:{score}", font15, brush, new Rectangle(20, 0, 560, 50), stringFormat);
                g.DrawString($"High Score:{UserSettings.Default.HighScore}", font15, brush, new Rectangle(20, 10, 870, 50));
            }

            roads.Take(20).Select((road, index) => new { road, index }).ToList().ForEach(
                item =>
                {
                    var fileName = $"{prefix}_road_{item.road.GetString()}".ToLower();
                    using Bitmap? image = rm.GetObject(fileName) as Bitmap;
                    if (image is null) return;
                    g.DrawImage(image, new Rectangle(item.index * 30, 200, 30, 50));
                }
            );

            var fileName = $"{prefix}_cat_{cat.GetString()}".ToLower();
            using Bitmap? image = rm.GetObject(fileName) as Bitmap;
            if (image is null) return;
            g.DrawImage(image, new Rectangle(120, 130, 120, 100));

            if (status != GameStatus.Playing)
            {
                using Brush fillBrush = new SolidBrush(Color.FromArgb(77, 0, 0, 0));
                g.FillRectangle(fillBrush, new Rectangle(0, 0, 600, 250));

                using Font font18 = new("Courier New", 18, FontStyle.Bold);
                using Brush brush = new SolidBrush(textColor);
                var message = "Press space to play.";
                if (status == GameStatus.GameOver)
                { 
                    if (score >= UserSettings.Default.HighScore)
                    {
                        SaveRecord(score);
                        message = "GAME OVER\nNew Record!!\n" + message;
                    }
                    else
                    {
                        message = "GAME OVER\n\n" + message;
                    }
                }
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(message, font18, brush, new Rectangle(0, 0, 600, 250), stringFormat);
                
            }
        }
        private void SaveRecord(int score)
        {
            UserSettings.Default.HighScore = score;
            UserSettings.Default.Save();
        }
    }

    internal static class ListExtension
    {
        internal static bool HasCommonElements(this List<int> list1, List<int> list2)
        {
            return list1.Intersect(list2).Any();
        }
    }
}
