using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using Flow.Launcher.Plugin.AliasFlow.Models;

namespace Flow.Launcher.Plugin.AliasFlow.Services;

public sealed class KeywordRepository
{
    // ✅ BOM 없는 UTF-8
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    // ✅ 한글 유니코드 이스케이프 방지 + 보기 좋게 들여쓰기
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null, // title/path/keywords 그대로 유지
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
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

        // BOM 있든 없든 정상 처리됨(없으면 Utf8NoBom으로 읽기)
        var json = File.ReadAllText(KeywordsJsonPath, Utf8NoBom);

        return JsonSerializer.Deserialize<List<KeywordEntry>>(json, JsonOptions)
               ?? new List<KeywordEntry>();
    }

    public void Save(IEnumerable<KeywordEntry> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(KeywordsJsonPath)!);

        var json = JsonSerializer.Serialize(items, JsonOptions);

        // ✅ BOM 없는 UTF-8로 저장
        File.WriteAllText(KeywordsJsonPath, json, Utf8NoBom);
    }

    public List<KeywordEntry> ImportFromFile(string filePath)
    {
        var json = File.ReadAllText(filePath, Utf8NoBom);

        return JsonSerializer.Deserialize<List<KeywordEntry>>(json, JsonOptions)
               ?? new List<KeywordEntry>();
    }

    public void ExportToFile(string filePath, IEnumerable<KeywordEntry> items)
    {
        var json = JsonSerializer.Serialize(items, JsonOptions);

        // ✅ BOM 없는 UTF-8로 저장
        File.WriteAllText(filePath, json, Utf8NoBom);
    }
}
