using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
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

    // 런타임에서 검색에 사용할 캐시
    private List<AliasItem> _cache = new();

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
            _cache = _repo.Load();
        };
    }

    // ✅ 이 메서드가 없어서 CS0535가 발생한 것
    public List<Result> Query(Query query)
    {
        // 사용자가 입력한 전체 문자열 (액션키워드 제외 문자열도 query에 들어있습니다)
        var raw = query?.Search ?? string.Empty;
        var q = raw.Trim();

        // 아무것도 입력 안 하면 전체 보여주기(원하면 비워도 됨)
        var items = string.IsNullOrEmpty(q)
            ? _cache
            : _cache.Where(x =>
                   (!string.IsNullOrWhiteSpace(x.Keyword) &&
                    x.Keyword.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrWhiteSpace(x.Description) &&
                    x.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrWhiteSpace(x.Target) &&
                    x.Target.Contains(q, StringComparison.OrdinalIgnoreCase))
              ).ToList();

        return items.Select(x => new Result
        {
            Title = x.Keyword,
            SubTitle = string.IsNullOrWhiteSpace(x.Description) ? x.DisplayTarget : $"{x.Description}  |  {x.DisplayTarget}",
            IcoPath = "icon.png",
            Action = _ =>
            {
                try
                {
                    // Target이 URL이면 기본 브라우저로
                    if (Uri.TryCreate(x.Target, UriKind.Absolute, out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    {
                        _context?.API.ShellRun(x.Target);
                        return true;
                    }

                    // 그 외는 실행 파일/바로가기/경로로 간주
                    _context?.API.ShellRun(x.Target, x.Arguments ?? string.Empty);
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
    // 초기화가 안 된 경우에도 Control 타입을 반환해야 함
    if (_settingsVm is null)
    {
        return new ContentControl
        {
            Content = "Settings not initialized."
        };
    }

    return new SettingsPanel(_settingsVm);
}

}
