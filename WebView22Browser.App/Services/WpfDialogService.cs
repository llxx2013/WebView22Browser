using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace WebView22Browser.App.Services;

public sealed class WpfDialogService : IDialogService
{
    public void ShowError(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowWarning(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void ShowInfo(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirm(string message, string title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public string? PromptSaveFile(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = "所有文件|*.*"
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickFolder(string description)
    {
        var dialog = new OpenFolderDialog
        {
            Title = description,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? PickOpenFile(string title, string filter)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = false
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public bool PromptCertificateError(string uri, string errorMessage) =>
        Confirm(
            $"站点证书存在问题：\n{errorMessage}\n\n地址：{uri}\n\n是否仍要继续访问？\n（同一会话内同一主机可能不再提示；重启应用后会再次询问。仅建议在开发/测试环境使用）",
            "证书错误");

    public bool PromptPermission(CoreWebView2PermissionKind kind, string origin)
    {
        var description = kind switch
        {
            CoreWebView2PermissionKind.Microphone => "麦克风",
            CoreWebView2PermissionKind.Camera => "摄像头",
            CoreWebView2PermissionKind.Geolocation => "地理位置",
            CoreWebView2PermissionKind.Notifications => "通知",
            CoreWebView2PermissionKind.OtherSensors => "其他传感器",
            _ => kind.ToString()
        };

        return Confirm(
            $"网站 {origin} 请求使用：{description}\n\n是否允许？",
            "权限请求");
    }
}
