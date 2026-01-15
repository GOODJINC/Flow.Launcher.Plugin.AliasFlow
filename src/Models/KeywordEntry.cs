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
    // - URL(https://...) 또는 로컬 실행 경로/명령
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    // keywords.json의 "keywords" (검색용 키워드 목록)
    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = new();

    // ✅ 추가: keywords.json의 "hotkey"
    // 예) "Ctrl+Shift+Space"
    // - path 대신(또는 함께) 전역 단축키를 트리거하고 싶을 때 사용
    [JsonPropertyName("hotkey")]
    public string Hotkey { get; set; } = "";

    // DataGrid 표시용 (JSON에는 저장 안 됨)
    [JsonIgnore]
    public string KeywordsDisplay =>
        Keywords is null || Keywords.Count == 0
            ? ""
            : string.Join(", ", Keywords);
}
