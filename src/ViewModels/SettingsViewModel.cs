using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Flow.Launcher.Plugin.AliasFlow.Models;
using Flow.Launcher.Plugin.AliasFlow.Services;

namespace Flow.Launcher.Plugin.AliasFlow.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly KeywordRepository _repo;

    public ObservableCollection<KeywordEntry> Items { get; } = new();

    private KeywordEntry? _selected;
    public KeywordEntry? Selected
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // 플러그인 런타임 캐시 갱신 트리거용
    public event EventHandler? KeywordsChanged;

    public SettingsViewModel(KeywordRepository repo)
    {
        _repo = repo;

        foreach (var it in _repo.Load())
            Items.Add(it);
    }

    public void Add(KeywordEntry item)
    {
        Validate(item, allowSameTitle: false);
        Items.Add(Normalize(item));
        Persist();
    }

    public void Update(KeywordEntry original, KeywordEntry edited)
    {
        var titleChanged = !string.Equals(original.Title, edited.Title, StringComparison.OrdinalIgnoreCase);
        Validate(edited, allowSameTitle: !titleChanged);

        var norm = Normalize(edited);

        original.Title = norm.Title;
        original.Path = norm.Path;
        original.Description = norm.Description;
        original.Keywords = norm.Keywords;

        Persist();
    }

    public void Delete(KeywordEntry item)
    {
        Items.Remove(item);
        Persist();
    }

    public void ReplaceAll(IEnumerable<KeywordEntry> newItems)
    {
        Items.Clear();
        foreach (var it in newItems.Select(Normalize))
            Items.Add(it);

        Persist();
    }

    // ✅ SettingsPanel.xaml.cs에서 호출 중 (없으면 컴파일 에러)
    public List<KeywordEntry> ImportFromFile(string path)
        => _repo.ImportFromFile(path);

    // ✅ SettingsPanel.xaml.cs에서 호출 중 (없으면 컴파일 에러)
    public void ExportToFile(string path)
        => _repo.ExportToFile(path, Items);

    public void Persist()
    {
        _repo.Save(Items);
        KeywordsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Validate(KeywordEntry item, bool allowSameTitle)
    {
        if (string.IsNullOrWhiteSpace(item.Title))
            throw new InvalidOperationException("Title은 비어 있을 수 없습니다.");

        if (string.IsNullOrWhiteSpace(item.Path))
            throw new InvalidOperationException("Path(URL/경로)는 비어 있을 수 없습니다.");

        if (!allowSameTitle)
        {
            var exists = Items.Any(x =>
                string.Equals(x.Title.Trim(), item.Title.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exists)
                throw new InvalidOperationException("이미 존재하는 Title입니다.");
        }
    }

    private static KeywordEntry Normalize(KeywordEntry item)
    {
        // null 방어 + trim + keywords 정리(중복 제거)
        var title = (item.Title ?? "").Trim();
        var path = (item.Path ?? "").Trim();
        var desc = (item.Description ?? "").Trim();

        var keywords = (item.Keywords ?? new List<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim())
            .Where(k => k.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new KeywordEntry
        {
            Title = title,
            Path = path,
            Description = desc,
            Keywords = keywords
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
