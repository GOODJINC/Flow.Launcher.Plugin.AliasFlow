// Views/SettingsPanel.xaml.cs
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Flow.Launcher.Plugin.AliasFlow.ViewModels;
using Flow.Launcher.Plugin.AliasFlow.Views;

namespace Flow.Launcher.Plugin.AliasFlow.Views;

public partial class SettingsPanel : UserControl
{
    private readonly SettingsViewModel _vm;

    public SettingsPanel(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = _vm;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var win = new AliasEditWindow { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() != true) return;

        try
        {
            _vm.Add(win.Result);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Add failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;

        // 원본 복사해서 편집
        var seed = new Flow.Launcher.Plugin.AliasFlow.Models.AliasItem
        {
            Keyword = _vm.Selected.Keyword,
            Target = _vm.Selected.Target,
            Arguments = _vm.Selected.Arguments,
            Description = _vm.Selected.Description
        };

        var win = new AliasEditWindow(seed) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() != true) return;

        try
        {
            _vm.Update(_vm.Selected, win.Result);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Edit failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;

        var ok = MessageBox.Show("선택한 항목을 삭제할까요?", "Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (ok != MessageBoxResult.Yes) return;

        _vm.Delete(_vm.Selected);
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            Title = "Import keywords JSON"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            // Repository는 VM 내부에서 쓰고 있으므로, VM에 ReplaceAll API를 둔 구조가 깔끔합니다.
            // 여기서는 간단히: 파일을 읽어서 ReplaceAll 호출(Repository 접근은 VM 생성부에서 주입)
            var repoField = typeof(SettingsViewModel)
                .GetField("_repo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (repoField?.GetValue(_vm) is not Flow.Launcher.Plugin.AliasFlow.Services.KeywordRepository repo)
                throw new InvalidOperationException("Repository 접근 실패");

            var imported = repo.ImportFromFile(dlg.FileName);
            _vm.ReplaceAll(imported);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            Title = "Export keywords JSON",
            FileName = "keywords.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var repoField = typeof(SettingsViewModel)
                .GetField("_repo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (repoField?.GetValue(_vm) is not Flow.Launcher.Plugin.AliasFlow.Services.KeywordRepository repo)
                throw new InvalidOperationException("Repository 접근 실패");

            repo.ExportToFile(dlg.FileName, _vm.Items);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
