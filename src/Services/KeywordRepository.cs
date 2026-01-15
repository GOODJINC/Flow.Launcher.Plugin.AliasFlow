using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Flow.Launcher.Plugin.AliasFlow.Models;

namespace Flow.Launcher.Plugin.AliasFlow.Services;

public sealed class KeywordRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,

        // ✅ 한글/일본어/중국어 등을 \uXXXX로 이스케이프하지 않도록
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string DataPath { get; }

    public KeywordRepository(string dataPath)
    {
        DataPath = dataPath;
    }

    public List<KeywordEntry> Load()
    {
        if (!File.Exists(DataPath))
            return new List<KeywordEntry>();

        var json = File.ReadAllText(DataPath, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<KeywordEntry>>(json, JsonOptions) ?? new List<KeywordEntry>();
    }

    public void Save(IEnumerable<KeywordEntry> items)
    {
        var dir = Path.GetDirectoryName(DataPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(items, JsonOptions);

        // ✅ BOM 없는 UTF-8 (메모장 호환)
        File.WriteAllText(DataPath, json, new UTF8Encoding(false));
    }

    public List<KeywordEntry> ImportFromFile(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<List<KeywordEntry>>(json, JsonOptions) ?? new List<KeywordEntry>();
    }

    public void ExportToFile(string path, IEnumerable<KeywordEntry> items)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(items, JsonOptions);
        File.WriteAllText(path, json, new UTF8Encoding(false));
    }
}
