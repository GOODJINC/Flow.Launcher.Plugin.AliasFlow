using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.AliasFlow.Models;

public sealed class KeywordEntry
{
    // keywords.json의 "title"
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    // keywords.json의 "description"
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    // keywords.json의 "path"
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    // keywords.json의 "keywords" (검색용 키워드 목록)
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    // DataGrid 표시용 (JSON에는 저장 안 됨)
    [JsonIgnore]
    public string KeywordsDisplay =>
        Keywords == null || Keywords.Count == 0
            ? ""
            : string.Join(", ", Keywords);
}
