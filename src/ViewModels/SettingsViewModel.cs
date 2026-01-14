using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        Items.Add(item);
        Persist();
    }

    public void Update(KeywordEntry original, KeywordEntry edited)
    {
        var titleChanged = !string.Equals(original.Title, edited.Title, StringComparison.OrdinalIgnoreCase);
        Validate(edited, allowSameTitle: !titleChanged);

        original.Title = edited.Title;
        original.Description = edited.Description;
        original.Path = edited.Path;
        original.Keywords = edited.Keywords ?? new();

        Persist();
    }

    public void Delete(KeywordEntry item)
    {
        Items.Remove(item);
        Persist();
    }

    public void ReplaceAll(System.Collections.Generic.IEnumerable<KeywordEntry> newItems)
    {
        Items.Clear();
        foreach (var it in newItems)
            Items.Add(it);
        Persist();
    }

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
            var exists = Items.Any(x => string.Equals(x.Title.Trim(), item.Title.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exists) throw new InvalidOperationException("이미 존재하는 Title입니다.");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
