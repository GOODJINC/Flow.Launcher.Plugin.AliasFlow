using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;

using Flow.Launcher.Plugin;

using Flow.Launcher.Plugin.AliasFlow.Models;
using Flow.Launcher.Plugin.AliasFlow.Services;
using Flow.Launcher.Plugin.AliasFlow.ViewModels;
using Flow.Launcher.Plugin.AliasFlow.Views;

namespace Flow.Launcher.Plugin.AliasFlow;

public sealed class Main : IPlugin, ISettingProvider
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
            pluginDir = AppDomain.CurrentDomain.BaseDirectory;

        _repo = new KeywordRepository(pluginDir);
        _settingsVm = new SettingsViewModel(_repo);

        _cache = _repo.Load();

        _settingsVm.KeywordsChanged += (_, __) =>
        {
            try
            {
                _cache = _repo.Load();
            }
            catch (Exception ex)
            {
                NotifyError("Alias Flow", $"Reload failed: {ex.Message}");
            }
        };
    }

    public Control CreateSettingPanel()
    {
        if (_settingsVm is null)
        {
            return new ContentControl { Content = "Settings not initialized." };
        }

        return new SettingsPanel(_settingsVm);
    }

    public List<Result> Query(Query query)
    {
        var qRaw = (query?.Search ?? "").Trim();
        IEnumerable<KeywordEntry> items = _cache;

        if (!string.IsNullOrEmpty(qRaw))
        {
            var isInitialQuery = KoreanInitialSearch.IsChoseongQuery(qRaw);
            var qInitial = isInitialQuery ? KoreanInitialSearch.NormalizeInitialQuery(qRaw) : "";

            if (isInitialQuery)
            {
                items = items
                    .Select(x => new { Item = x, Score = GetInitialScore(x, qInitial) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Item.Title, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Item);
            }
            else
            {
                items = items.Where(x =>
                    ContainsIgnoreCase(x.Title, qRaw) ||
                    ContainsIgnoreCase(x.Description, qRaw) ||
                    ContainsIgnoreCase(x.Path, qRaw) ||
                    ContainsIgnoreCase(x.Hotkey, qRaw) ||
                    (x.Keywords?.Any(k => ContainsIgnoreCase(k, qRaw)) ?? false)
                );
            }
        }

        return items.Select(x => new Result
        {
            Title = x.Title,
            SubTitle = BuildSubtitle(x),
            IcoPath = "icon.png",
            Action = _ => ExecuteEntry(x)
        }).ToList();
    }

    private bool ExecuteEntry(KeywordEntry x)
    {
        try
        {
            // 1) Hotkey 우선

            var hk = (x.Hotkey ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(hk))
            {
                var ok = HotkeySender.TrySendHotkey(hk, out var err);
                if (ok) return true;
                NotifyError("Alias Flow", $"Hotkey failed: {hk}\n{err}");
                // hotkey 실패해도 path가 있으면 이어서 시도
            }


            // 2) Path 실행
            var rawPath = (x.Path ?? "").Trim();
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                // hotkey도 없고 path도 없으면 실행할 게 없음
                NotifyError("Alias Flow", "No action: both hotkey and path are empty.");
                return false;
            }

            var expanded = ExpandAndClean(rawPath);

            // URL이면 기본 브라우저로
            if (Uri.TryCreate(expanded, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return TryOpenWithShellExecute(expanded, args: null, workingDir: null);
            }

            // 파일/폴더/프로그램(+args) 실행
            ParseExecutableAndArgs(rawPath, out var exe, out var args);
            exe = ExpandAndClean(exe);
            args = (args ?? "").Trim();

            if (string.IsNullOrWhiteSpace(exe))
            {
                NotifyError("Alias Flow", $"Invalid path: {rawPath}");
                return false;
            }

            // working dir 추정
            string? wd = null;
            if (File.Exists(exe))
                wd = Path.GetDirectoryName(exe);
            else if (Directory.Exists(exe))
                wd = exe;

            // 핵심: UseShellExecute=true로 OS에 맡김 (프로그램/파일/폴더/링크 모두)
            var opened = TryOpenWithShellExecute(exe, args, wd);
            if (!opened)
            {
                NotifyError("Alias Flow", $"Launch failed: {exe} {args}".Trim());
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            NotifyError("Alias Flow", $"Execute error: {ex.Message}");
            return false;
        }
    }

    private static string BuildSubtitle(KeywordEntry x)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(x.Description))
            parts.Add(x.Description.Trim());

        if (!string.IsNullOrWhiteSpace(x.Path))
            parts.Add(x.Path.Trim());

        if (!string.IsNullOrWhiteSpace(x.Hotkey))
            parts.Add($"Hotkey: {x.Hotkey.Trim()}");

        return parts.Count == 0 ? "" : string.Join("  |  ", parts);
    }

    private void NotifyError(string title, string message)
    {
        try
        {
            _context?.API?.ShowMsg(title, message);
        }
        catch
        {
            // Notification 자체가 실패해도 플러그인이 죽지 않게 무시
        }
    }

    private static bool ContainsIgnoreCase(string? haystack, string needle)
    {
        if (string.IsNullOrWhiteSpace(haystack)) return false;
        return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------
    // 실행 관련 유틸
    // -------------------------

    private static string ExpandAndClean(string input)
    {
        var s = (input ?? "").Trim();

        // 양끝 따옴표 제거
        if (s.Length >= 2 && s.StartsWith("\"", StringComparison.Ordinal) && s.EndsWith("\"", StringComparison.Ordinal))
            s = s.Substring(1, s.Length - 2);

        // 환경변수 확장 (%USERNAME% 등)
        s = Environment.ExpandEnvironmentVariables(s);

        return s.Trim();
    }

    private static bool TryOpenWithShellExecute(string fileOrUrl, string? args, string? workingDir)
    {
        try
        {
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

    /// <summary>
    /// 공백 포함 경로를 따옴표 없이 넣어도 실행되도록:
    /// - 전체가 파일/폴더면 그대로 exe
    /// - 아니면 토큰을 누적하며 실제 존재하는 경로를 찾고 나머지를 args로 분리
    /// - 마지막 fallback: 첫 토큰 exe, 나머지 args
    /// </summary>
    private static void ParseExecutableAndArgs(string raw, out string exe, out string args)
    {
        exe = "";
        args = "";

        var s = ExpandAndClean(raw);
        if (string.IsNullOrWhiteSpace(s))
            return;

        // 전체가 파일/폴더면 그대로
        if (File.Exists(s) || Directory.Exists(s))
        {
            exe = s;
            args = "";
            return;
        }

        // "C:\...\app.exe" args...
        if (s.StartsWith("\"", StringComparison.Ordinal))
        {
            var end = s.IndexOf('"', 1);
            if (end > 1)
            {
                exe = ExpandAndClean(s.Substring(1, end - 1));
                args = s.Substring(end + 1).Trim();
                return;
            }
        }

        // 공백 토큰 분리
        var tokens = Regex.Matches(s, @"[^\s]+").Select(m => m.Value).ToList();
        if (tokens.Count == 0) return;

        // 누적하며 존재하는 경로 찾기
        for (int i = 0; i < tokens.Count; i++)
        {
            var candidate = string.Join(" ", tokens.Take(i + 1));
            candidate = ExpandAndClean(candidate);

            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                exe = candidate;
                args = string.Join(" ", tokens.Skip(i + 1));
                return;
            }
        }

        // 마지막 fallback
        exe = ExpandAndClean(tokens[0]);
        args = string.Join(" ", tokens.Skip(1));
    }

    // -------------------------
    // 초성 검색 유틸 + 점수
    // -------------------------

    private static int GetInitialScore(KeywordEntry x, string qInitial)
    {
        if (string.IsNullOrWhiteSpace(qInitial)) return 0;

        // Title 우선
        var titleInitial = KoreanInitialSearch.ToInitialString(x.Title ?? "");
        if (!string.IsNullOrWhiteSpace(titleInitial))
        {
            if (titleInitial.StartsWith(qInitial, StringComparison.Ordinal)) return 500;
            if (titleInitial.Contains(qInitial, StringComparison.Ordinal)) return 400;
        }

        // Keywords
        if (x.Keywords is not null)
        {
            foreach (var k in x.Keywords)
            {
                var ki = KoreanInitialSearch.ToInitialString(k ?? "");
                if (ki.Contains(qInitial, StringComparison.Ordinal)) return 300;
            }
        }

        // Description
        var di = KoreanInitialSearch.ToInitialString(x.Description ?? "");
        if (di.Contains(qInitial, StringComparison.Ordinal)) return 200;

        // Path
        var pi = KoreanInitialSearch.ToInitialString(x.Path ?? "");
        if (pi.Contains(qInitial, StringComparison.Ordinal)) return 100;

        // Hotkey도 초성 검색 대상에 넣을 이유는 거의 없지만, 원하면 확장 가능
        return 0;
    }
}

// 파일 내에 같이 둬도 되고, 별도 파일로 분리해도 됨
internal static class KoreanInitialSearch
{
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

    public static string NormalizeInitialQuery(string q)
    {
        return new string((q ?? "")
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
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
                int choseongIndex = syllableIndex / 588; // 21*28
                sb.Append(Choseong[choseongIndex]);
            }
            else
            {
                // 한글이 아니면 그대로(영문/숫자 등)
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
