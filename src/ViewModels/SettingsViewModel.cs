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

    public event PropertyChangedEventHandler? PropertyChanged;

    // ✅ Main.cs가 (sender, e) 2개 인수로 구독 가능
    public event EventHandler? KeywordsChanged;

    public ObservableCollection<KeywordEntry> Items { get; } = new();

    private KeywordEntry? _selected;
    public KeywordEntry? Selected
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value)) return;
            _selected = value;
            OnPropertyChanged();
        }
    }

    public SettingsViewModel(KeywordRepository repo)
    {
        _repo = repo;

        var loaded = _repo.Load();
        foreach (var item in loaded)
            Items.Add(Normalize(item));

        Selected = Items.FirstOrDefault();
    }

    // -------------------------
    // CRUD
    // -------------------------
    public KeywordEntry Add(KeywordEntry entry)
    {
        var n = Normalize(entry);
        Items.Add(n);
        Selected = n;
        SaveAndNotify();
        return n;
    }

    public void Delete(KeywordEntry entry)
    {
        Items.Remove(entry);
        if (ReferenceEquals(Selected, entry))
            Selected = Items.FirstOrDefault();
        SaveAndNotify();
    }

    public KeywordEntry Update(KeywordEntry oldItem, KeywordEntry newItem)
    {
        if (oldItem is null) throw new ArgumentNullException(nameof(oldItem));
        if (newItem is null) throw new ArgumentNullException(nameof(newItem));

        var n = Normalize(newItem);

        // ✅ UI 즉시 갱신: "교체" 방식
        var idx = Items.IndexOf(oldItem);
        if (idx >= 0)
        {
            Items[idx] = n;
            Selected = n;
        }
        else
        {
            Items.Add(n);
            Selected = n;
        }

        SaveAndNotify();
        return n;
    }

    public void ReplaceAll(System.Collections.Generic.IEnumerable<KeywordEntry> entries)
    {
        Items.Clear();
        foreach (var e in entries.Select(Normalize))
            Items.Add(e);

        Selected = Items.FirstOrDefault();
        SaveAndNotify();
    }

    // -------------------------
    // Import/Export passthrough
    // -------------------------
    public System.Collections.Generic.List<KeywordEntry> ImportFromFile(string path)
        => _repo.ImportFromFile(path);

    public void ExportToFile(string path)
        => _repo.ExportToFile(path, Items);

    // -------------------------
    // internal
    // -------------------------
    private void SaveAndNotify()
    {
        _repo.Save(Items);
        KeywordsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static KeywordEntry Normalize(KeywordEntry e)
    {
        var title = (e.Title ?? "").Trim();
        var desc = (e.Description ?? "").Trim();
        var path = (e.Path ?? "").Trim();
        var hotkey = (e.Hotkey ?? "").Trim();

        var keywords = (e.Keywords ?? new())
            .Select(x => (x ?? "").Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new KeywordEntry
        {
            Title = title,
            Description = desc,
            Path = path,
            Hotkey = hotkey,
            Keywords = keywords
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
