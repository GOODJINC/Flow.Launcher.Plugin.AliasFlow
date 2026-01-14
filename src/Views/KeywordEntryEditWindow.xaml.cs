using System;
using System.Linq;
using System.Windows;
using Flow.Launcher.Plugin.AliasFlow.Models;

namespace Flow.Launcher.Plugin.AliasFlow.Views;

public partial class KeywordEntryEditWindow : Window
{
    public KeywordEntry Result { get; private set; } = new();

    public KeywordEntryEditWindow(KeywordEntry? seed = null)
    {
        InitializeComponent();

        if (seed is null) return;

        TitleBox.Text = seed.Title ?? "";
        PathBox.Text = seed.Path ?? "";
        DescBox.Text = seed.Description ?? "";
        KeywordsBox.Text = seed.Keywords is null ? "" : string.Join(", ", seed.Keywords);
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var title = (TitleBox.Text ?? "").Trim();
        var path = (PathBox.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorText.Text = "Title은 필수입니다.";
            return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            ErrorText.Text = "Path는 필수입니다.";
            return;
        }

        var keywords = (KeywordsBox.Text ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Result = new KeywordEntry
        {
            Title = title,
            Path = path,
            Description = (DescBox.Text ?? "").Trim(),
            Keywords = keywords
        };

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
