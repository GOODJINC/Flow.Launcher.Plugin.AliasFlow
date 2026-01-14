using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.AliasFlow.Models;
using Flow.Launcher.Plugin.AliasFlow.Services;
using Flow.Launcher.Plugin.AliasFlow.ViewModels;
using Flow.Launcher.Plugin.AliasFlow.Views;

namespace Flow.Launcher.Plugin.AliasFlow;

public class Main : IPlugin, ISettingProvider
{
    private PluginInitContext? _context;
    private KeywordRepository? _repo;
    private SettingsViewModel? _settingsVm;

    private List<KeywordEntry> _cache = new();

    public void Init(PluginInitContext context)
    {
        _context = context;

        var pluginDir = context.CurrentPluginMetadata?.PluginDirectory;
        if (string.IsNullOrWhiteSpace(pluginDir))
            pluginDir = AppDomain.CurrentDomain.BaseDirectory; // fallback

        _repo = new KeywordRepository(pluginDir);
        _settingsVm = new SettingsViewModel(_repo);

        _cache = _repo.Load();

        _settingsVm.KeywordsChanged += (_, __) =>
        {
            if (_repo is not null)
                _cache = _repo.Load();
        };
    }

    public List<Result> Query(Query query)
{
    var qRaw = (query?.Search ?? "").Trim();
    IEnumerable<KeywordEntry> items = _cache;

    // 초성 쿼리 판정
    var isInitialQuery = KoreanInitialSearch.IsChoseongQuery(qRaw);
    var qInitial = isInitialQuery ? KoreanInitialSearch.NormalizeInitialQuery(qRaw) : "";

    // 초성일 때는 스코어링해서 정렬
    if (!string.IsNullOrEmpty(qRaw))
    {
        if (isInitialQuery)
        {
            items = items
                .Select(x => new
                {
                    Item = x,
                    Score = GetInitialScore(x, qInitial)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.Item);
        }
        else
        {
            // 일반 검색(기존 로직)
            items = items.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Title) && x.Title.Contains(qRaw, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Description) && x.Description.Contains(qRaw, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Path) && x.Path.Contains(qRaw, StringComparison.OrdinalIgnoreCase)) ||
                (x.Keywords?.Any(k => !string.IsNullOrWhiteSpace(k) && k.Contains(qRaw, StringComparison.OrdinalIgnoreCase)) ?? false)
            );
        }
    }

    return items.Select(x => new Result
    {
        Title = x.Title,
        SubTitle = string.IsNullOrWhiteSpace(x.Description) ? x.Path : $"{x.Description}  |  {x.Path}",
        IcoPath = "icon.png",
        Action = _ =>
{
    try
    {
        var raw = (x.Path ?? "").Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        // URL은 그대로 ShellExecute로 오픈
        var expanded = ExpandAndClean(raw);
        if (Uri.TryCreate(expanded, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = expanded,
                UseShellExecute = true
            });
            return true;
        }

        // 파일/프로그램/폴더 실행: 공백 포함 경로를 깨지 않게 파싱
        ParseExecutableAndArgs(raw, out var exe, out var args);
        exe = ExpandAndClean(exe);
        args = (args ?? "").Trim();

        if (string.IsNullOrWhiteSpace(exe))
            return false;

        // working dir 추정
        string? wd = null;
        if (File.Exists(exe))
            wd = Path.GetDirectoryName(exe);
        else if (Directory.Exists(exe))
            wd = exe;

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(wd) && Directory.Exists(wd))
            psi.WorkingDirectory = wd;

        Process.Start(psi);
        return true;
    }
    catch
    {
        return false;
    }
}


    }).ToList();
}

    public Control CreateSettingPanel()
    {
        if (_settingsVm is null)
        {
            return new ContentControl
            {
                Content = "Settings not initialized."
            };
        }

        return new SettingsPanel(_settingsVm);
    }

    private static void SplitCommand(string command, out string exe, out string args)
    {
        command = (command ?? "").Trim();

        if (command.StartsWith("\"", StringComparison.Ordinal))
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

    internal static class KoreanInitialSearch
{
    // 한글 초성 테이블 (19개)
    private static readonly char[] Choseong =
    {
        'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ','ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
    };

    private static readonly HashSet<char> ChoseongSet = new(Choseong);

    public static bool IsChoseongQuery(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return false;

        foreach (var c in q)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (!ChoseongSet.Contains(c)) return false;
        }
        return true;
    }

    public static string ToInitialString(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var sb = new StringBuilder(text.Length);

        foreach (var c in text)
        {
            // 한글 음절 범위: AC00–D7A3
            if (c >= 0xAC00 && c <= 0xD7A3)
            {
                int syllableIndex = c - 0xAC00;
                int choseongIndex = syllableIndex / 588; // 21*28=588
                sb.Append(Choseong[choseongIndex]);
            }
            else
            {
                // 한글 음절이 아니면 그대로 넣거나(원하면 제외해도 됨)
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    public static bool MatchByInitial(string q, params string?[] fields)
    {
        if (string.IsNullOrWhiteSpace(q)) return false;

        // 공백 제거한 초성 쿼리
        var query = new string(q.Where(c => !char.IsWhiteSpace(c)).ToArray());

        foreach (var f in fields)
        {
            if (string.IsNullOrWhiteSpace(f)) continue;

            var initials = ToInitialString(f);
            if (initials.Contains(query, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static string NormalizeInitialQuery(string q)
{
    return new string((q ?? "")
        .Where(c => !char.IsWhiteSpace(c))
        .ToArray());
}

}
private static int GetInitialScore(KeywordEntry x, string qInitial)
{
    if (string.IsNullOrWhiteSpace(qInitial)) return 0;

    // Title
    var title = x.Title ?? "";
    var titleInitial = KoreanInitialSearch.ToInitialString(title);

    if (!string.IsNullOrWhiteSpace(titleInitial))
    {
        // 1) Title 초성 prefix 일치 (최상)
        if (titleInitial.StartsWith(qInitial, StringComparison.Ordinal))
            return 500;

        // 2) Title 초성 contains (차상)
        if (titleInitial.Contains(qInitial, StringComparison.Ordinal))
            return 400;
    }

    // 3) Keywords 초성 일치
    if (x.Keywords is not null)
    {
        foreach (var k in x.Keywords)
        {
            if (string.IsNullOrWhiteSpace(k)) continue;
            var ki = KoreanInitialSearch.ToInitialString(k);
            if (ki.Contains(qInitial, StringComparison.Ordinal))
                return 300;
        }
    }

    // 4) Description 초성 일치
    var desc = x.Description ?? "";
    if (!string.IsNullOrWhiteSpace(desc))
    {
        var di = KoreanInitialSearch.ToInitialString(desc);
        if (di.Contains(qInitial, StringComparison.Ordinal))
            return 200;
    }

    // 5) Path 초성 일치 (보통 한글이 적지만, 포함)
    var path = x.Path ?? "";
    if (!string.IsNullOrWhiteSpace(path))
    {
        var pi = KoreanInitialSearch.ToInitialString(path);
        if (pi.Contains(qInitial, StringComparison.Ordinal))
            return 100;
    }

    return 0;
}
private static bool TryOpenWithShellExecute(string fileOrUrl, string? args = null, string? workingDir = null)
{
    try
    {
        fileOrUrl = (fileOrUrl ?? "").Trim();
        args = (args ?? "").Trim();

        if (string.IsNullOrWhiteSpace(fileOrUrl))
            return false;

        // 폴더면 탐색기로 열기
        if (Directory.Exists(fileOrUrl))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileOrUrl,
                UseShellExecute = true
            });
            return true;
        }

        // 파일/프로그램/URL 모두 ShellExecute로 처리
        var psi = new ProcessStartInfo
        {
            FileName = fileOrUrl,
            UseShellExecute = true
        };

        if (!string.IsNullOrWhiteSpace(args))
            psi.Arguments = args;

        if (!string.IsNullOrWhiteSpace(workingDir) && Directory.Exists(workingDir))
            psi.WorkingDirectory = workingDir;

        Process.Start(psi);
        return true;
    }
    catch
    {
        return false;
    }
}

// 기존 SplitCommand를 활용하되, 실행 시 working dir도 추정
private static string? GuessWorkingDirectory(string exePath)
{
    try
    {
        if (string.IsNullOrWhiteSpace(exePath)) return null;

        // 상대경로/환경변수 확장된 경우도 고려
        exePath = Environment.ExpandEnvironmentVariables(exePath);

        // 파일이 존재하면 그 폴더를 working dir로
        if (File.Exists(exePath))
            return Path.GetDirectoryName(exePath);

        return null;
    }
    catch
    {
        return null;
    }
}
private static string ExpandAndClean(string input)
{
    var s = (input ?? "").Trim();

    // 앞뒤 따옴표 제거
    if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
        s = s.Substring(1, s.Length - 2);

    // %USERNAME% 같은 환경변수 확장
    s = Environment.ExpandEnvironmentVariables(s);

    return s.Trim();
}
private static void ParseExecutableAndArgs(string raw, out string exe, out string args)
{
    exe = "";
    args = "";

    var s = ExpandAndClean(raw);
    if (string.IsNullOrWhiteSpace(s))
        return;

    // 1) 전체 문자열이 이미 파일/폴더 경로라면 그대로 실행(공백 포함해도 OK)
    if (File.Exists(s) || Directory.Exists(s))
    {
        exe = s;
        args = "";
        return;
    }

    // 2) 따옴표로 감싼 exe "C:\...\app.exe" args...
    if (s.StartsWith("\""))
    {
        // 이 경우 ExpandAndClean에서 바깥따옴표가 제거되므로 여기 들어올 확률은 낮지만,
        // 혹시 남아있다면 처리
        var end = s.IndexOf('"', 1);
        if (end > 1)
        {
            exe = s.Substring(1, end - 1);
            args = s.Substring(end + 1).Trim();
            exe = ExpandAndClean(exe);
            return;
        }
    }

    // 3) 공백으로 split 후, "존재하는 파일"이 될 때까지 앞 토큰을 늘려가며 후보 검사
    //    예: C:\Program Files\Kakao\KakaoTalk\KakaoTalk.exe
    var tokens = Regex.Matches(s, @"[^\s]+").Select(m => m.Value).ToList();
    if (tokens.Count == 0) return;

    for (int i = 0; i < tokens.Count; i++)
    {
        var candidate = string.Join(" ", tokens.Take(i + 1));
        candidate = ExpandAndClean(candidate);

        // 후보가 실제 파일/폴더이면 확정
        if (File.Exists(candidate) || Directory.Exists(candidate))
        {
            exe = candidate;
            args = string.Join(" ", tokens.Skip(i + 1));
            return;
        }

        // 후보가 .exe/.lnk/.cmd/.bat 등으로 끝나면(경로는 맞지만 아직 존재 판단 실패할 수 있음)
        // 다음 단계로 넘기되, 마지막에 fallback로 사용 가능
    }

    // 4) fallback: 기존 방식(첫 토큰 exe, 나머지 args)
    exe = ExpandAndClean(tokens[0]);
    args = string.Join(" ", tokens.Skip(1));
}

}
