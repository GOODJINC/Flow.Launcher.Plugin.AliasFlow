// Models/AliasItem.cs
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.AliasFlow.Models;

public sealed class AliasItem
{
    public string Keyword { get; set; } = "";
    public string Target { get; set; } = "";      // URL 또는 exe/lnk 경로 등
    public string? Arguments { get; set; }        // 선택
    public string? Description { get; set; }      // 선택

    [JsonIgnore]
    public string DisplayTarget => string.IsNullOrWhiteSpace(Arguments) ? Target : $"{Target} {Arguments}";
}
