using System.Windows;
using System.Windows.Controls;

using WebView22Browser.App.Services;
using WebView22Browser.Core.Models;

namespace WebView22Browser.App.Views;

public partial class UserScriptEditWindow : Window
{
    private readonly UserScriptEntry _entry;
    private readonly IDialogService _dialogService;

    public UserScriptEditWindow(UserScriptEntry entry, bool isNew, IDialogService dialogService)
    {
        _entry = entry;
        _dialogService = dialogService;
        InitializeComponent();
        Title = isNew ? "添加用户脚本" : "编辑用户脚本";

        runAtBox.ItemsSource = new[]
        {
            new RunAtItem("document-start", UserScriptRunAt.DocumentStart),
            new RunAtItem("document-end", UserScriptRunAt.DocumentEnd),
            new RunAtItem("document-idle", UserScriptRunAt.DocumentIdle)
        };
        runAtBox.DisplayMemberPath = nameof(RunAtItem.Label);
        runAtBox.SelectedValuePath = nameof(RunAtItem.Value);

        nameBox.Text = entry.Name;
        patternsBox.Text = string.Join(Environment.NewLine, entry.MatchPatterns);
        excludeBox.Text = string.Join(Environment.NewLine, entry.ExcludePatterns);
        codeBox.Text = entry.Code;
        enabledBox.IsChecked = entry.Enabled;
        topFrameBox.IsChecked = entry.RunInTopFrameOnly;
        if (runAtBox.ItemsSource is IEnumerable<RunAtItem> items)
            runAtBox.SelectedItem = items.FirstOrDefault(i => i.Value == entry.RunAt) ?? items.First();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var name = nameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            _dialogService.ShowWarning("请输入脚本名称。", Title);
            return;
        }

        var patterns = SplitLines(patternsBox.Text);
        if (patterns.Length == 0)
        {
            _dialogService.ShowWarning("请至少输入一条匹配规则。", Title);
            return;
        }

        _entry.Name = name;
        _entry.MatchPatterns = patterns;
        _entry.ExcludePatterns = SplitLines(excludeBox.Text);
        _entry.Code = codeBox.Text;
        _entry.Enabled = enabledBox.IsChecked == true;
        _entry.RunInTopFrameOnly = topFrameBox.IsChecked == true;
        _entry.RunAt = runAtBox.SelectedItem is RunAtItem runAtItem
            ? runAtItem.Value
            : UserScriptRunAt.DocumentStart;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string[] SplitLines(string text) =>
        text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToArray();

    private sealed record RunAtItem(string Label, UserScriptRunAt Value);
}