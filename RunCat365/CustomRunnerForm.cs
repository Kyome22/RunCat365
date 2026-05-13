using RunCat365.Properties;
using System.Drawing.Drawing2D;
using FormsTimer = System.Windows.Forms.Timer;

namespace RunCat365
{
    internal class CustomRunnerForm : Form
    {
        private readonly CustomRunnerRepository repository;
        private readonly Action revertToBuiltInRunner;
        private readonly ListBox runnerListBox;
        private readonly TextBox nameTextBox;
        private readonly Label nameWarningLabel;
        private readonly FlowLayoutPanel framePanel;
        private readonly Button addFramesButton;
        private readonly Button removeFrameButton;
        private readonly Button moveFrameUpButton;
        private readonly Button moveFrameDownButton;
        private readonly Button saveButton;
        private readonly Button deleteButton;
        private readonly PictureBox previewPictureBox;
        private readonly TrackBar speedSlider;
        private readonly FormsTimer previewTimer;

        private readonly List<Bitmap> pendingFrames = [];
        private readonly List<Bitmap> recoloredFrames = []; // For accessibility and preview
        private int currentPreviewFrame = 0;
        private int selectedFrameIndex = -1;

        internal CustomRunnerForm(
            CustomRunnerRepository repository,
            Action revertToBuiltInRunner
        )
        {
            this.repository = repository;
            this.revertToBuiltInRunner = revertToBuiltInRunner;

            Text = Strings.Window_CustomRunners;
            Icon = Resources.AppIcon;
            ClientSize = new Size(700, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(45, 45, 45);
            ForeColor = Color.White;

            // Problem 5: Perfect visual alignment mapping based on fixed ClientSize 700x480
            // Margins: 12px padding around container edges
            var listLabel = new Label
            {
                Text = Strings.CustomRunner_AddedRunners,
                Location = new Point(12, 12),
                Size = new Size(160, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            runnerListBox = new ListBox
            {
                Location = new Point(12, 35),
                Size = new Size(160, 391), // Clean margin to bottom buttons (480 - 12 - 30 - 12) = 426 -> 35 + 391 = 426
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            runnerListBox.SelectedIndexChanged += RunnerListBoxSelectedIndexChanged;

            var nameLabel = new Label
            {
                Text = Strings.CustomRunner_Name,
                Location = new Point(184, 12),
                Size = new Size(100, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9F)
            };

            nameTextBox = new TextBox
            {
                Location = new Point(284, 10),
                Size = new Size(404, 24), // Extends up to 700 - 12 = 688
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 30
            };
            nameTextBox.TextChanged += (s, e) => ValidateFormState();

            nameWarningLabel = new Label
            {
                Text = "⚠️ Profile exists. Saving will overwrite.",
                Location = new Point(448, 40),
                Size = new Size(240, 18),
                ForeColor = Color.FromArgb(220, 160, 60),
                Font = new Font("Segoe UI", 7.5F),
                TextAlign = ContentAlignment.TopRight,
                Visible = false
            };

            var requirementsLabel = new Label
            {
                Text = Strings.CustomRunner_Requirements,
                Location = new Point(184, 40),
                Size = new Size(400, 18),
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Segoe UI", 7.5F)
            };

            addFramesButton = new ThemedButton
            {
                Text = Strings.CustomRunner_AddFrames,
                Location = new Point(184, 64),
                Size = new Size(110, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand
            };
            addFramesButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            addFramesButton.Click += AddFramesButtonClick;

            var clearFramesButton = new ThemedButton
            {
                Text = Strings.CustomRunner_ClearFrames,
                Location = new Point(304, 64),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand
            };
            clearFramesButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            clearFramesButton.Click += ClearFramesButtonClick;

            removeFrameButton = new ThemedButton
            {
                Text = Strings.CustomRunner_RemoveFrame,
                Location = new Point(404, 64),
                Size = new Size(95, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            removeFrameButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            removeFrameButton.Click += RemoveFrameButtonClick;

            moveFrameUpButton = new ThemedButton
            {
                Text = Strings.CustomRunner_MoveFrameUp,
                Location = new Point(509, 64),
                Size = new Size(55, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            moveFrameUpButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            moveFrameUpButton.Click += MoveFrameUpButtonClick;

            moveFrameDownButton = new ThemedButton
            {
                Text = Strings.CustomRunner_MoveFrameDown,
                Location = new Point(574, 64),
                Size = new Size(65, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            moveFrameDownButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            moveFrameDownButton.Click += MoveFrameDownButtonClick;

            framePanel = new FlowLayoutPanel
            {
                Location = new Point(184, 100),
                Size = new Size(350, 326), // Bottom perfectly aligns with bottom of listbox (100+326 = 426)
                AutoScroll = true,
                BackColor = Color.FromArgb(55, 55, 55),
                BorderStyle = BorderStyle.FixedSingle,
                WrapContents = true
            };

            // Problem 3: Preview Panel Configuration
            var previewLabel = new Label
            {
                Text = "Preview:",
                Location = new Point(546, 100),
                Size = new Size(142, 18),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 8.5F)
            };

            previewPictureBox = new PictureBox
            {
                Location = new Point(546, 122),
                Size = new Size(142, 142),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(55, 55, 55),
                SizeMode = PictureBoxSizeMode.CenterImage
            };

            previewTimer = new FormsTimer { Interval = 100 };
            previewTimer.Tick += PreviewTimerTick;

            var speedLabel = new Label
            {
                Text = "Preview Speed:",
                Location = new Point(546, 276),
                Size = new Size(142, 18),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 8.5F)
            };

            speedSlider = new TrackBar
            {
                Location = new Point(546, 298),
                Size = new Size(142, 45),
                Minimum = 1,
                Maximum = 10,
                Value = 5,
                TickStyle = TickStyle.None
            };
            speedSlider.ValueChanged += (s, e) => {
                if (speedSlider.Value > 0)
                {
                    previewTimer.Interval = Math.Max(20, 500 / speedSlider.Value);
                }
            };

            deleteButton = new ThemedButton
            {
                Text = Strings.CustomRunner_Delete,
                Location = new Point(12, 438), // 480 - 12 - 30
                Size = new Size(75, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            deleteButton.FlatAppearance.BorderColor = Color.FromArgb(150, 60, 60);
            deleteButton.Click += DeleteButtonClick;

            saveButton = new ThemedButton
            {
                Text = Strings.CustomRunner_Save,
                Location = new Point(603, 438), // 700 - 12 - 85
                Size = new Size(85, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 90, 160),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false // Disabled by default for Problem 2 validation
            };
            saveButton.FlatAppearance.BorderColor = Color.FromArgb(70, 120, 200);
            saveButton.Click += SaveButtonClick;

            Controls.AddRange([
                listLabel,
                runnerListBox,
                nameLabel,
                nameTextBox,
                nameWarningLabel,
                requirementsLabel,
                addFramesButton,
                clearFramesButton,
                removeFrameButton,
                moveFrameUpButton,
                moveFrameDownButton,
                framePanel,
                previewLabel,
                previewPictureBox,
                speedLabel,
                speedSlider,
                deleteButton,
                saveButton
            ]);

            RefreshRunnerList();
            ValidateFormState();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            previewTimer.Stop();
            base.OnFormClosing(e);
            ClearPendingFrames();
        }

        private void RefreshRunnerList()
        {
            runnerListBox.Items.Clear();
            foreach (var profile in repository.GetAll())
            {
                runnerListBox.Items.Add(profile.Name);
            }
            UpdateRunnerActionButtons();
        }

        private void RunnerListBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            var hasSelection = runnerListBox.SelectedIndex >= 0;
            UpdateRunnerActionButtons();

            if (hasSelection && runnerListBox.SelectedItem is string selectedName)
            {
                nameTextBox.Text = selectedName;
                ClearPendingFrames();
                var frames = repository.LoadFrames(selectedName);
                pendingFrames.AddRange(frames);
                selectedFrameIndex = pendingFrames.Count > 0 ? 0 : -1;
                RefreshFramePanel();
            }
        }

        private void AddFramesButtonClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "PNG Images|*.png",
                Multiselect = true,
                Title = Strings.CustomRunner_AddFrames
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var sortedFiles = dialog.FileNames.OrderBy(f => f).ToArray();
            foreach (var file in sortedFiles)
            {
                if (pendingFrames.Count >= 30) break;
                try
                {
                    pendingFrames.Add(new Bitmap(file));
                }
                catch { }
            }
            RefreshFramePanel();
        }

        private void ClearFramesButtonClick(object? sender, EventArgs e)
        {
            ClearPendingFrames();
            RefreshFramePanel();
        }

        private void RemoveFrameButtonClick(object? sender, EventArgs e)
        {
            if (selectedFrameIndex < 0 || pendingFrames.Count <= selectedFrameIndex) return;

            pendingFrames[selectedFrameIndex].Dispose();
            pendingFrames.RemoveAt(selectedFrameIndex);
            selectedFrameIndex = pendingFrames.Count == 0
                ? -1
                : Math.Min(selectedFrameIndex, pendingFrames.Count - 1);
            RefreshFramePanel();
        }

        private void MoveFrameUpButtonClick(object? sender, EventArgs e)
        {
            if (selectedFrameIndex <= 0 || pendingFrames.Count <= selectedFrameIndex) return;

            (pendingFrames[selectedFrameIndex - 1], pendingFrames[selectedFrameIndex]) =
                (pendingFrames[selectedFrameIndex], pendingFrames[selectedFrameIndex - 1]);
            selectedFrameIndex -= 1;
            RefreshFramePanel();
        }

        private void MoveFrameDownButtonClick(object? sender, EventArgs e)
        {
            if (selectedFrameIndex < 0 || pendingFrames.Count - 1 <= selectedFrameIndex) return;

            (pendingFrames[selectedFrameIndex], pendingFrames[selectedFrameIndex + 1]) =
                (pendingFrames[selectedFrameIndex + 1], pendingFrames[selectedFrameIndex]);
            selectedFrameIndex += 1;
            RefreshFramePanel();
        }

        private void RefreshFramePanel()
        {
            framePanel.SuspendLayout();
            framePanel.Controls.Clear();
            previewTimer.Stop();
            currentPreviewFrame = 0;
            previewPictureBox.Image = null;
            if (pendingFrames.Count <= selectedFrameIndex)
            {
                selectedFrameIndex = pendingFrames.Count - 1;
            }

            // Problem 4: Recolor internal silhouettes so they don't blend into dark mode backgrounds
            foreach (var frame in recoloredFrames)
            {
                frame.Dispose();
            }
            recoloredFrames.Clear();

            for (int i = 0; i < pendingFrames.Count; i++)
            {
                // Generate a white-silhouette contrast representation dynamically
                var recolored = pendingFrames[i].Recolor(Color.White);
                recoloredFrames.Add(recolored);

                var container = new Panel
                {
                    Size = new Size(70, 76),
                    Margin = new Padding(4),
                    BackColor = i == selectedFrameIndex
                        ? Color.FromArgb(75, 95, 125)
                        : Color.Transparent
                };

                var frameIndex = i;
                container.Click += (sender, e) => SelectFrame(frameIndex);

                var pictureBox = new PictureBox
                {
                    Size = new Size(48, 48),
                    Location = new Point(11, 0),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = recolored,
                    BackColor = Color.FromArgb(40, 40, 40)
                };
                pictureBox.Click += (sender, e) => SelectFrame(frameIndex);

                var indexLabel = new Label
                {
                    Text = $"{i}",
                    Size = new Size(70, 18),
                    Location = new Point(0, 52),
                    TextAlign = ContentAlignment.TopCenter,
                    ForeColor = Color.FromArgb(170, 170, 170),
                    Font = new Font("Segoe UI", 7.5F)
                };
                indexLabel.Click += (sender, e) => SelectFrame(frameIndex);

                container.Controls.Add(pictureBox);
                container.Controls.Add(indexLabel);
                framePanel.Controls.Add(container);
            }

            framePanel.ResumeLayout();

            if (recoloredFrames.Count > 0)
            {
                previewPictureBox.Image = recoloredFrames[0];
                if (recoloredFrames.Count > 1)
                {
                    previewTimer.Interval = Math.Max(20, 500 / speedSlider.Value);
                    previewTimer.Start();
                }
            }
            ValidateFormState();
        }

        private void SelectFrame(int frameIndex)
        {
            if (frameIndex < 0 || pendingFrames.Count <= frameIndex) return;
            selectedFrameIndex = frameIndex;
            RefreshFramePanel();
        }

        private void PreviewTimerTick(object? sender, EventArgs e)
        {
            if (recoloredFrames.Count < 2) return;
            currentPreviewFrame = (currentPreviewFrame + 1) % recoloredFrames.Count;
            previewPictureBox.Image = recoloredFrames[currentPreviewFrame];
            previewPictureBox.Invalidate();
        }

        // Problem 2: Real-time UI validation explicitly preventing bad inputs immediately
        private void ValidateFormState()
        {
            var name = nameTextBox.Text.Trim();
            bool hasValidName = !string.IsNullOrWhiteSpace(name);
            bool hasValidFrames = pendingFrames.Count >= 2;

            saveButton.Enabled = hasValidName && hasValidFrames;

            nameWarningLabel.Visible = hasValidName && repository.Exists(name);
            UpdateFrameActionButtons();
        }

        private void UpdateFrameActionButtons()
        {
            var hasSelectedFrame = 0 <= selectedFrameIndex && selectedFrameIndex < pendingFrames.Count;
            removeFrameButton.Enabled = hasSelectedFrame;
            moveFrameUpButton.Enabled = hasSelectedFrame && selectedFrameIndex > 0;
            moveFrameDownButton.Enabled = hasSelectedFrame && selectedFrameIndex < pendingFrames.Count - 1;
        }

        private void UpdateRunnerActionButtons()
        {
            deleteButton.Enabled = runnerListBox.SelectedIndex >= 0;
        }

        private void SaveButtonClick(object? sender, EventArgs e)
        {
            var name = nameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            if (pendingFrames.Count < 2) return;

            if (repository.Exists(name))
            {
                var result = MessageBox.Show(
                    Strings.CustomRunner_ConfirmOverwrite,
                    Strings.Message_Warning,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (result != DialogResult.Yes) return;
            }

            if (repository.Save(name, pendingFrames))
            {
                RefreshRunnerList();
                for (int i = 0; i < runnerListBox.Items.Count; i++)
                {
                    if (runnerListBox.Items[i] is string itemName &&
                        itemName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        runnerListBox.SelectedIndex = i;
                        break;
                    }
                }
                ValidateFormState();
            }
        }

        private void DeleteButtonClick(object? sender, EventArgs e)
        {
            if (runnerListBox.SelectedItem is not string selectedName) return;

            var result = MessageBox.Show(
                string.Format(Strings.CustomRunner_ConfirmDelete, selectedName),
                Strings.Message_Warning,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );
            if (result != DialogResult.Yes) return;

            repository.Delete(selectedName);
            revertToBuiltInRunner();
            ClearPendingFrames();
            RefreshFramePanel();
            nameTextBox.Clear();
            RefreshRunnerList();
            UpdateRunnerActionButtons();
        }

        private void ClearPendingFrames()
        {
            previewTimer.Stop();
            previewPictureBox.Image = null;
            foreach (var frame in pendingFrames)
            {
                frame.Dispose();
            }
            pendingFrames.Clear();
            foreach (var frame in recoloredFrames)
            {
                frame.Dispose();
            }
            recoloredFrames.Clear();
            selectedFrameIndex = -1;
            UpdateFrameActionButtons();
        }

        private sealed class ThemedButton : Button
        {
            private static readonly Color DisabledBackColor = Color.FromArgb(58, 58, 58);
            private static readonly Color DisabledForeColor = Color.FromArgb(145, 145, 145);
            private static readonly Color DisabledBorderColor = Color.FromArgb(82, 82, 82);

            protected override void OnEnabledChanged(EventArgs e)
            {
                base.OnEnabledChanged(e);
                Cursor = Enabled ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var backColor = Enabled ? BackColor : DisabledBackColor;
                var foreColor = Enabled ? ForeColor : DisabledForeColor;
                var borderColor = Enabled ? FlatAppearance.BorderColor : DisabledBorderColor;

                using var backBrush = new SolidBrush(backColor);
                using var borderPen = new Pen(borderColor);
                e.Graphics.FillRectangle(backBrush, ClientRectangle);
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    ClientRectangle,
                    foreColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                );
            }
        }
    }
}
