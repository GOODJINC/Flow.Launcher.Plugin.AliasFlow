using System;
using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.AliasFlow.Models;

private List<KeywordEntry> _cache = new();

public List<Result> Query(Query query)
{
    var q = (query?.Search ?? "").Trim();

    IEnumerable<KeywordEntry> items = _cache;

    if (!string.IsNullOrEmpty(q))
    {
        items = items.Where(x =>
            (!string.IsNullOrWhiteSpace(x.Title) && x.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(x.Description) && x.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(x.Path) && x.Path.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
            (x.Keywords?.Any(k => k?.Contains(q, StringComparison.OrdinalIgnoreCase) == true) ?? false)
        );
    }

    return items.Select(x => new Result
    {
        Title = x.Title,
        SubTitle = string.IsNullOrWhiteSpace(x.Description) ? x.Path : $"{x.Description}  |  {x.Path}",
        IcoPath = "icon.png",
        Action = _ =>
        {
            var raw = Environment.ExpandEnvironmentVariables(x.Path ?? "");
            if (string.IsNullOrWhiteSpace(raw)) return false;

            // 특수 명령(예: open_config_folder)
            if (string.Equals(raw, "open_config_folder", StringComparison.OrdinalIgnoreCase))
            {
                // KeywordsJsonPath 폴더 열기 등 (원하는 동작으로 구현)
                // _context?.API.ShellRun(<folderPath>);
                return true;
            }

            if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                _context?.API.ShellRun(raw);
                return true;
            }

            // "exe args..." 형태면 분리 (가장 단순한 방식: 첫 토큰을 exe로)
            SplitCommand(raw, out var exe, out var args);
            _context?.API.ShellRun(exe, args);
            return true;
        }
    }).ToList();
}

private static void SplitCommand(string command, out string exe, out string args)
{
    // 아주 단순한 분리: 공백 기준 첫 토큰
    // 경로가 따옴표로 감싸진 경우를 처리
    command = command.Trim();

    if (command.StartsWith("\""))
    {
        var end = command.IndexOf("\"", 1, StringComparison.Ordinal);
        if (end > 1)
        {
            exe = command.Substring(1, end - 1);
            args = command.Substring(end + 1).Trim();
            return;
        }
    }

    var firstSpace = command.IndexOf(' ');
    if (firstSpace < 0)
    {
        exe = command;
        args = "";
        return;
    }

    exe = command.Substring(0, firstSpace).Trim();
    args = command.Substring(firstSpace + 1).Trim();
}
