﻿// Copyright 2020 Takuto Nakamura
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
        private readonly Label scoreLabel;
        private readonly Label frontLabel;
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
            Text = "Endless Game";
            Icon = Resources.AppIcon;
            BackColor = systemTheme == Theme.Light ? Color.Gainsboro : Color.Gray;
            StartPosition = FormStartPosition.CenterScreen;

            scoreLabel = new Label
            {
                Text = "Score: 0",
                Font = new Font("Courier New", 15),
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(20, 0),
                Size = new Size(560, 50)
            };
            Controls.Add(scoreLabel);

            frontLabel = new Label
            {
                Text = "Press space to play.",
                Font = new Font("Courier New", 18, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(),
                Size = new Size(600, 250),
                BackColor = Color.FromArgb(77, 0, 0, 0)
            };
            Controls.Add(frontLabel);
            frontLabel.BringToFront();

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
                frontLabel.Text = "Game Over\n\nPress space to play.";
                Controls.Add(frontLabel);
                frontLabel.BringToFront();
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
                scoreLabel.Text = $"Score: {score}";
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
            if (!Judge()) return;
            UpdateRoads();
            UpdateCat();
            AutoJump();
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
                        Controls.Remove(frontLabel);
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

            var clipRegion = new Rectangle(0, 50, 600, 200);
            g.SetClip(clipRegion);

            var rm = Resources.ResourceManager;
            var prefix = systemTheme.GetString();

            roads.Take(20).Select((road, index) => new { road, index }).ToList().ForEach(
                item =>
                {
                    var fileName = $"{prefix}_road_{item.road.GetString()}".ToLower();
                    using Bitmap? image = rm.GetObject(fileName) as Bitmap;
                    if (image is null) return;
                    g.DrawImage(image, new Rectangle(new Point(item.index * 30, 200), new Size(30, 50)));
                }
            );

            var fileName = $"{prefix}_cat_{cat.GetString()}".ToLower();
            using Bitmap? image = rm.GetObject(fileName) as Bitmap;
            if (image is null) return;
            g.DrawImage(image, new Rectangle(new Point(120, 130), new Size(120, 100)));
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
