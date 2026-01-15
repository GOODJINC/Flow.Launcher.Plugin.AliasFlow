using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

using Flow.Launcher.Plugin.AliasFlow.Models;
using Flow.Launcher.Plugin.AliasFlow.ViewModels;

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

    // -----------------------------
    // Toolbar
    // -----------------------------
    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var win = new KeywordEntryEditWindow(null) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() != true) return;

        try
        {
            var added = _vm.Add(win.Result);
            _vm.Selected = added;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Add failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void Delete_Click(object sender, RoutedEventArgs e) => DeleteSelected();

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            Title = "Import JSON"
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
            Title = "Export JSON",
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

    // -----------------------------
    // List UX
    // -----------------------------
    private void ItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        EditSelected();
    }

    private void ItemsList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            EditSelected();
            return;
        }

        if (e.Key == Key.Delete)
        {
            e.Handled = true;
            DeleteSelected();
            return;
        }
    }

    // -----------------------------
    // 리스트 edge에서 부모 ScrollViewer로 휠 전달
    // -----------------------------
    private void ItemsList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ListView lv) return;

        var inner = FindDescendantScrollViewer(lv);
        if (inner is null) return;

        bool up = e.Delta > 0;
        bool atTop = inner.VerticalOffset <= 0;
        bool atBottom = inner.VerticalOffset >= inner.ScrollableHeight;

        if ((up && !atTop) || (!up && !atBottom))
            return;

        var outer = FindParentScrollViewer(lv);
        if (outer is null) return;

        e.Handled = true;

        const double step = 60.0;
        var next = outer.VerticalOffset - Math.Sign(e.Delta) * step;
        outer.ScrollToVerticalOffset(Math.Max(0, Math.Min(outer.ScrollableHeight, next)));
    }

    private void EditSelected()
    {
        if (_vm.Selected is null) return;

        var seed = new KeywordEntry
        {
            Title = _vm.Selected.Title ?? "",
            Description = _vm.Selected.Description ?? "",
            Path = _vm.Selected.Path ?? "",
            Hotkey = _vm.Selected.Hotkey ?? "",
            Keywords = _vm.Selected.Keywords is null ? new() : new(_vm.Selected.Keywords)
        };

        var win = new KeywordEntryEditWindow(seed) { Owner = Window.GetWindow(this) };
        if (win.ShowDialog() != true) return;

        try
        {
            var updated = _vm.Update(_vm.Selected, win.Result);
            _vm.Selected = updated;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Edit failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DeleteSelected()
    {
        if (_vm.Selected is null) return;

        var ok = MessageBox.Show("선택한 항목을 삭제할까요?", "Delete",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (ok != MessageBoxResult.Yes) return;

        try
        {
            _vm.Delete(_vm.Selected);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Delete failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    // -----------------------------
    // VisualTree helpers
    // -----------------------------
    private static ScrollViewer? FindParentScrollViewer(DependencyObject start)
    {
        var p = VisualTreeHelper.GetParent(start);
        while (p != null)
        {
            if (p is ScrollViewer sv) return sv;
            p = VisualTreeHelper.GetParent(p);
        }
        return null;
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject start)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(start); i++)
        {
            var c = VisualTreeHelper.GetChild(start, i);
            if (c is ScrollViewer sv) return sv;

            var found = FindDescendantScrollViewer(c);
            if (found != null) return found;
        }
        return null;
    }
}
