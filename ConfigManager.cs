using System.Text.Json;
using System.IO;
public static class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AiReviewer", "config.json");

    public static string? GetApiKey()
    {
        // Сначала пробуем переменную окружения (для запуска из терминала)
        var envKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
        if (!string.IsNullOrEmpty(envKey)) return envKey;

        // Потом читаем из файла конфига
        if (!File.Exists(ConfigPath)) return null;
        try
        {
            var json = File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (config != null && config.TryGetValue("ApiKey", out var key))
            return key;
            return null;
        }
        catch { return null; }
    }

    public static void SaveApiKey(string key)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        var config = new Dictionary<string, string> { ["ApiKey"] = key };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config));
    }

    public static bool HasApiKey() => !string.IsNullOrEmpty(GetApiKey());
}