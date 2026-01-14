using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Flow.Launcher.Plugin.AliasFlow.Models;
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
        // KeywordEntry 편집 (Title/Path/Description/Keywords)
        var win = new KeywordEntryEditWindow(null) { Owner = Window.GetWindow(this) };
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

        // 원본 복사본으로 편집
        var seed = new KeywordEntry
        {
            Title = _vm.Selected.Title,
            Path = _vm.Selected.Path,
            Description = _vm.Selected.Description,
            Keywords = _vm.Selected.Keywords is null ? new() : new(_vm.Selected.Keywords)
        };

        var win = new KeywordEntryEditWindow(seed) { Owner = Window.GetWindow(this) };
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
            Title = "Import keywords.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _vm.ReplaceAll(_vm.ImportFromFile(dlg.FileName));
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
            Title = "Export keywords.json",
            FileName = "keywords.json"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _vm.ExportToFile(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
