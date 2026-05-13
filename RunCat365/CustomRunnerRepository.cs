using System.Text.Json;

namespace RunCat365
{
    internal class CustomRunnerRepository
    {
        private const int MAX_FRAME_HEIGHT = 32;
        private const int MIN_FRAME_WIDTH = 10;
        private const int MAX_FRAME_WIDTH = 32;
        private const int MAX_FRAME_COUNT = 30;
        private const string PROFILES_FILE_NAME = "profiles.json";

        private readonly string basePath;
        private List<CustomRunnerProfile> profiles = [];

        internal CustomRunnerRepository()
        {
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RunCat365",
                "CustomRunners"
            );
            Directory.CreateDirectory(basePath);
            Load();
        }

        internal List<CustomRunnerProfile> GetAll()
        {
            return [.. profiles];
        }

        internal CustomRunnerProfile? GetByName(string name)
        {
            return profiles.Find(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        internal List<Bitmap> LoadFrames(string name)
        {
            var profile = GetByName(name);
            if (profile is null) return [];

            var runnerDirectory = Path.Combine(basePath, SanitizeDirectoryName(name));
            var frames = new List<Bitmap>();
            foreach (var fileName in profile.FrameFileNames)
            {
                var filePath = Path.Combine(runnerDirectory, fileName);
                if (!File.Exists(filePath)) continue;
                frames.Add(new Bitmap(filePath));
            }
            return frames;
        }

        internal bool Save(string name, List<Bitmap> frames)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (frames.Count < 2 || frames.Count > MAX_FRAME_COUNT) return false;

            var runnerDirectory = Path.Combine(basePath, SanitizeDirectoryName(name));
            Directory.CreateDirectory(runnerDirectory);

            var existingProfile = GetByName(name);
            if (existingProfile is not null)
            {
                DeleteFrameFiles(existingProfile);
                profiles.Remove(existingProfile);
            }

            var profile = new CustomRunnerProfile { Name = name };
            for (int i = 0; i < frames.Count; i++)
            {
                var resized = ResizeFrame(frames[i]);
                var fileName = $"frame_{i}.png";
                var filePath = Path.Combine(runnerDirectory, fileName);
                resized.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                profile.FrameFileNames.Add(fileName);
                if (resized != frames[i]) resized.Dispose();
            }

            profiles.Add(profile);
            SaveProfiles();
            return true;
        }

        internal bool Delete(string name)
        {
            var profile = GetByName(name);
            if (profile is null) return false;

            DeleteFrameFiles(profile);
            var runnerDirectory = Path.Combine(basePath, SanitizeDirectoryName(name));
            try
            {
                if (Directory.Exists(runnerDirectory))
                {
                    Directory.Delete(runnerDirectory, true);
                }
            }
            catch { }

            profiles.Remove(profile);
            SaveProfiles();
            return true;
        }

        internal bool Exists(string name)
        {
            return GetByName(name) is not null;
        }

        private void Load()
        {
            var profilesPath = Path.Combine(basePath, PROFILES_FILE_NAME);
            if (!File.Exists(profilesPath))
            {
                profiles = [];
                return;
            }
            try
            {
                var json = File.ReadAllText(profilesPath);
                profiles = JsonSerializer.Deserialize<List<CustomRunnerProfile>>(json) ?? [];
            }
            catch
            {
                profiles = [];
            }
        }

        private void SaveProfiles()
        {
            var profilesPath = Path.Combine(basePath, PROFILES_FILE_NAME);
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(profiles, options);
            File.WriteAllText(profilesPath, json);
        }

        private void DeleteFrameFiles(CustomRunnerProfile profile)
        {
            var runnerDirectory = Path.Combine(basePath, SanitizeDirectoryName(profile.Name));
            foreach (var fileName in profile.FrameFileNames)
            {
                var filePath = Path.Combine(runnerDirectory, fileName);
                try { File.Delete(filePath); } catch { }
            }
        }

        private static Bitmap ResizeFrame(Bitmap original)
        {
            if (original.Height <= MAX_FRAME_HEIGHT &&
                original.Width >= MIN_FRAME_WIDTH &&
                original.Width <= MAX_FRAME_WIDTH)
            {
                return original;
            }

            var scale = (float)MAX_FRAME_HEIGHT / original.Height;
            var newWidth = Math.Clamp((int)(original.Width * scale), MIN_FRAME_WIDTH, MAX_FRAME_WIDTH);
            var newHeight = MAX_FRAME_HEIGHT;

            var resized = new Bitmap(newWidth, newHeight);
            using var graphics = Graphics.FromImage(resized);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(original, 0, 0, newWidth, newHeight);
            return resized;
        }

        private static string SanitizeDirectoryName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            return sanitized.ToLowerInvariant();
        }
    }
}
