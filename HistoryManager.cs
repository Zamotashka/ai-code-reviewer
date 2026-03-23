using System.IO;
using System.Text.Json;

public class HistoryEntry
{
    public string FileName { get; set; } = "";
    public string Review { get; set; } = "";
    public DateTime Date { get; set; }
}

public static class HistoryManager
{
    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AiReviewer", "history.json");

    public static void Save(string fileName, string review)
    {
        var entries = Load();
        entries.Insert(0, new HistoryEntry
        {
            FileName = fileName,
            Review = review,
            Date = DateTime.Now
        });

        if (entries.Count > 50)
            entries = entries.Take(50).ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(HistoryPath)!);
        File.WriteAllText(HistoryPath, JsonSerializer.Serialize(entries));
    }

    public static List<HistoryEntry> Load()
    {
        if (!File.Exists(HistoryPath)) return new List<HistoryEntry>();
        try
        {
            var json = File.ReadAllText(HistoryPath);
            return JsonSerializer.Deserialize<List<HistoryEntry>>(json) ?? new();
        }
        catch { return new(); }
    }
    public static void Clear()
    {
        if (File.Exists(HistoryPath))
            File.Delete(HistoryPath);
    }
}