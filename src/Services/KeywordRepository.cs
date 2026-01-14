using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Flow.Launcher.Plugin.AliasFlow.Models;

namespace Flow.Launcher.Plugin.AliasFlow.Services;

public sealed class KeywordRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,  // json 키를 그대로 유지 (title/path/keywords)
        WriteIndented = true
    };

    public string KeywordsJsonPath { get; }

    public KeywordRepository(string pluginDirectory)
    {
        KeywordsJsonPath = Path.Combine(pluginDirectory, "keywords.json");
    }

    public List<KeywordEntry> Load()
    {
        if (!File.Exists(KeywordsJsonPath))
            return new List<KeywordEntry>();

        var json = File.ReadAllText(KeywordsJsonPath, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<KeywordEntry>>(json, JsonOptions) ?? new List<KeywordEntry>();
    }

    public void Save(IEnumerable<KeywordEntry> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(KeywordsJsonPath)!);
        var json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(KeywordsJsonPath, json, Encoding.UTF8);
    }

    public List<KeywordEntry> ImportFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<KeywordEntry>>(json, JsonOptions) ?? new List<KeywordEntry>();
    }

    public void ExportToFile(string filePath, IEnumerable<KeywordEntry> items)
    {
        var json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}
