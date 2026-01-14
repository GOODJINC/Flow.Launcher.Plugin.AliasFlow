// Views/AliasEditWindow.xaml.cs
using System.Windows;
using Flow.Launcher.Plugin.AliasFlow.Models;

namespace Flow.Launcher.Plugin.AliasFlow.Views;

public partial class AliasEditWindow : Window
{
    public AliasItem Result { get; private set; } = new();

    public AliasEditWindow(AliasItem? seed = null)
    {
        InitializeComponent();

        if (seed is null) return;

        KeywordBox.Text = seed.Keyword;
        TargetBox.Text = seed.Target;
        ArgsBox.Text = seed.Arguments ?? "";
        DescBox.Text = seed.Description ?? "";
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var keyword = KeywordBox.Text.Trim();
        var target = TargetBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            ErrorText.Text = "Keyword는 필수입니다.";
            return;
        }
        if (string.IsNullOrWhiteSpace(target))
        {
            ErrorText.Text = "Target은 필수입니다.";
            return;
        }

        Result = new AliasItem
        {
            Keyword = keyword,
            Target = target,
            Arguments = string.IsNullOrWhiteSpace(ArgsBox.Text) ? null : ArgsBox.Text.Trim(),
            Description = string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim(),
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
