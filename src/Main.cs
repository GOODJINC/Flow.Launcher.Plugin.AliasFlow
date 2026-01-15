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

        // (A) 플러그인 설치 폴더(읽기 전용일 수 있음): icon.png / 기본 keywords.json이 들어있는 위치
        var pluginDir = context.CurrentPluginMetadata?.PluginDirectory;
        if (string.IsNullOrWhiteSpace(pluginDir))
            pluginDir = AppDomain.CurrentDomain.BaseDirectory;

        // (B) ✅ 사용자 데이터 폴더(쓰기 보장): keywords.json 저장/로드는 무조건 여기서만
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dataDir = Path.Combine(appData, "FlowLauncher", "Settings", "Plugins", "AliasFlow");
        var dataPath = Path.Combine(dataDir, "keywords.json");

        // (C) 최초 1회: 패키징된 기본 keywords.json이 있으면 사용자 데이터 경로로 복사
        try
        {
            if (!File.Exists(dataPath))
            {
                var packaged = Path.Combine(pluginDir, "keywords.json");
                if (File.Exists(packaged))
                {
                    Directory.CreateDirectory(dataDir);
                    File.Copy(packaged, dataPath, overwrite: false);
                }
                else
                {
                    // 기본 파일도 없으면 빈 파일로 시작(Repository.Save에서 폴더 생성)
                    Directory.CreateDirectory(dataDir);
                }
            }
        }
        catch (Exception ex)
        {
            NotifyError("Alias Flow", $"Init file setup failed: {ex.Message}");
        }

        // (D) ✅ Repository는 dataPath 기반으로 동작
        _repo = new KeywordRepository(dataPath);
        _settingsVm = new SettingsViewModel(_repo);

        // (E) 캐시 로드
        try
        {
            _cache = _repo.Load();
        }
        catch (Exception ex)
        {
            _cache = new();
            NotifyError("Alias Flow", $"Load failed: {ex.Message}");
        }

        // (F) 설정 변경 시 캐시 리로드
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
            // ignore
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

        if (s.Length >= 2 && s.StartsWith("\"", StringComparison.Ordinal) && s.EndsWith("\"", StringComparison.Ordinal))
            s = s.Substring(1, s.Length - 2);

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

    private static void ParseExecutableAndArgs(string raw, out string exe, out string args)
    {
        exe = "";
        args = "";

        var s = ExpandAndClean(raw);
        if (string.IsNullOrWhiteSpace(s))
            return;

        if (File.Exists(s) || Directory.Exists(s))
        {
            exe = s;
            args = "";
            return;
        }

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

        var tokens = Regex.Matches(s, @"[^\s]+").Select(m => m.Value).ToList();
        if (tokens.Count == 0) return;

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

        exe = ExpandAndClean(tokens[0]);
        args = string.Join(" ", tokens.Skip(1));
    }

    // -------------------------
    // 초성 검색 유틸 + 점수
    // -------------------------

    private static int GetInitialScore(KeywordEntry x, string qInitial)
    {
        if (string.IsNullOrWhiteSpace(qInitial)) return 0;

        var titleInitial = KoreanInitialSearch.ToInitialString(x.Title ?? "");
        if (!string.IsNullOrWhiteSpace(titleInitial))
        {
            if (titleInitial.StartsWith(qInitial, StringComparison.Ordinal)) return 500;
            if (titleInitial.Contains(qInitial, StringComparison.Ordinal)) return 400;
        }

        if (x.Keywords is not null)
        {
            foreach (var k in x.Keywords)
            {
                var ki = KoreanInitialSearch.ToInitialString(k ?? "");
                if (ki.Contains(qInitial, StringComparison.Ordinal)) return 300;
            }
        }

        var di = KoreanInitialSearch.ToInitialString(x.Description ?? "");
        if (di.Contains(qInitial, StringComparison.Ordinal)) return 200;

        var pi = KoreanInitialSearch.ToInitialString(x.Path ?? "");
        if (pi.Contains(qInitial, StringComparison.Ordinal)) return 100;

        return 0;
    }
}

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
            if (c >= 0xAC00 && c <= 0xD7A3)
            {
                int syllableIndex = c - 0xAC00;
                int choseongIndex = syllableIndex / 588;
                sb.Append(Choseong[choseongIndex]);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
