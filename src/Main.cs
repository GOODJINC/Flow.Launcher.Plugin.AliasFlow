using System;
using System.Collections.Generic;
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
        var q = (query?.Search ?? "").Trim();

        IEnumerable<KeywordEntry> items = _cache;

        if (!string.IsNullOrEmpty(q))
        {
            items = items.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Title) && x.Title.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Description) && x.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Path) && x.Path.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                (x.Keywords?.Any(k => !string.IsNullOrWhiteSpace(k) && k.Contains(q, StringComparison.OrdinalIgnoreCase)) ?? false)
            );
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
                    var raw = Environment.ExpandEnvironmentVariables(x.Path ?? "");
                    if (string.IsNullOrWhiteSpace(raw)) return false;

                    if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    {
                        _context?.API.ShellRun(raw);
                        return true;
                    }

                    SplitCommand(raw, out var exe, out var args);
                    _context?.API.ShellRun(exe, args);
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
}
