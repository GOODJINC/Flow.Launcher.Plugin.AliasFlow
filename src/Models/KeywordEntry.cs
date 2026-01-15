using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.AliasFlow.Models;

public sealed class KeywordEntry
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    // URL 또는 exe 경로
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    // NEW: hotkey 문자열 (예: "Ctrl+Shift+Space")
    [JsonPropertyName("hotkey")]
    public string Hotkey { get; set; } = "";

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    // UI 표시용 (JSON에는 저장되지 않음)
    [JsonIgnore]
    public string KeywordsDisplay => Keywords is null ? "" : string.Join(", ", Keywords);
}
