// ViewModels/SettingsViewModel.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Flow.Launcher.Plugin.AliasFlow.Models;
using Flow.Launcher.Plugin.AliasFlow.Services;

namespace Flow.Launcher.Plugin.AliasFlow.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly KeywordRepository _repo;

    public ObservableCollection<AliasItem> Items { get; } = new();

    private AliasItem? _selected;
    public AliasItem? Selected
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // 플러그인 본체가 구독해서 런타임 캐시 갱신에 사용
    public event EventHandler? KeywordsChanged;

    public SettingsViewModel(KeywordRepository repo)
    {
        _repo = repo;

        foreach (var it in _repo.Load())
            Items.Add(it);
    }

    public void Add(AliasItem item)
    {
        Validate(item, allowSameKeyword: false);
        Items.Add(item);
        Persist();
    }

    public void Update(AliasItem original, AliasItem edited)
    {
        // keyword 변경 가능하게 할 경우 중복 체크 필요
        var keywordChanged = !string.Equals(original.Keyword, edited.Keyword, StringComparison.OrdinalIgnoreCase);
        Validate(edited, allowSameKeyword: !keywordChanged);

        original.Keyword = edited.Keyword;
        original.Target = edited.Target;
        original.Arguments = edited.Arguments;
        original.Description = edited.Description;

        Persist();
    }

    public void Delete(AliasItem item)
    {
        Items.Remove(item);
        Persist();
    }

    public void Persist()
    {
        _repo.Save(Items);
        KeywordsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ReplaceAll(IEnumerable<AliasItem> newItems)
    {
        Items.Clear();
        foreach (var it in newItems)
            Items.Add(it);

        Persist();
    }

    private void Validate(AliasItem item, bool allowSameKeyword)
    {
        if (string.IsNullOrWhiteSpace(item.Keyword))
            throw new InvalidOperationException("Keyword는 비어 있을 수 없습니다.");

        if (string.IsNullOrWhiteSpace(item.Target))
            throw new InvalidOperationException("Target(URL/경로)는 비어 있을 수 없습니다.");

        if (!allowSameKeyword)
        {
            var exists = Items.Any(x =>
                string.Equals(x.Keyword.Trim(), item.Keyword.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exists)
                throw new InvalidOperationException("이미 존재하는 Keyword입니다.");
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
