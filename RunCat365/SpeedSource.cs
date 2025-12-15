namespace RunCat365
{
    internal enum SpeedSource
    {
        CPU,
        GPU,
        Memory
    }

    internal static class SpeedSourceExtension
    {
        internal static string GetString(this SpeedSource source)
        {
            return source switch
            {
                SpeedSource.CPU => "CPU",
                SpeedSource.GPU => "GPU",
                SpeedSource.Memory => "Memory",
                _ => "CPU"
            };
        }

        internal static bool TryParse(string? value, out SpeedSource source)
        {
            source = value switch
            {
                "GPU" => SpeedSource.GPU,
                "Memory" => SpeedSource.Memory,
                _ => SpeedSource.CPU
            };
            return true;
        }
    }
}
