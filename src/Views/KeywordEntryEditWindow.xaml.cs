using System;
using System.Linq;
using System.Windows;
using Flow.Launcher.Plugin.AliasFlow.Models;

namespace Flow.Launcher.Plugin.AliasFlow.Views;

public partial class KeywordEntryEditWindow : Window
{
    public KeywordEntry Result { get; private set; } = new();

    public KeywordEntryEditWindow(KeywordEntry? seed)
    {
        InitializeComponent();

        if (seed != null)
        {
            TitleBox.Text = seed.Title ?? "";
            DescBox.Text = seed.Description ?? "";
            PathBox.Text = seed.Path ?? "";
            HotkeyBox.Text = seed.Hotkey ?? "";
            KeywordsBox.Text = seed.Keywords is null ? "" : string.Join(", ", seed.Keywords);
        }
    }

    private void HotkeyClearButton_Click(object sender, RoutedEventArgs e)
    {
        HotkeyBox.Text = "";
        HotkeyBox.Focus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var title = (TitleBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Title is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var keywords = (KeywordsBox.Text ?? "")
            .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Result = new KeywordEntry
        {
            Title = title,
            Description = (DescBox.Text ?? "").Trim(),
            Path = (PathBox.Text ?? "").Trim(),
            Hotkey = (HotkeyBox.Text ?? "").Trim(),
            Keywords = keywords
        };

        DialogResult = true;
        Close();
    }
}
