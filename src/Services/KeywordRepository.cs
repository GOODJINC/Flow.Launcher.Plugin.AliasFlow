using System;
using System.Collections.Generic;
using System.IO;              // ✅ 추가
using System.Text;
using System.Text.Json;
using Flow.Launcher.Plugin.AliasFlow.Models;

namespace Flow.Launcher.Plugin.AliasFlow.Services;

public sealed class KeywordRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string KeywordsJsonPath { get; }

    public KeywordRepository(string pluginDirectory)
    {
        KeywordsJsonPath = Path.Combine(pluginDirectory, "keywords.json");
    }

    public List<AliasItem> Load()
    {
        if (!File.Exists(KeywordsJsonPath))
            return new List<AliasItem>();

        var json = File.ReadAllText(KeywordsJsonPath, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<AliasItem>>(json, JsonOptions) ?? new List<AliasItem>();
    }

    public void Save(IEnumerable<AliasItem> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(KeywordsJsonPath)!);
        var json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(KeywordsJsonPath, json, Encoding.UTF8);
    }

    public List<AliasItem> ImportFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<AliasItem>>(json, JsonOptions) ?? new List<AliasItem>();
    }

    public void ExportToFile(string filePath, IEnumerable<AliasItem> items)
    {
        var json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(filePath, json, Encoding.UTF8);
    }
}
