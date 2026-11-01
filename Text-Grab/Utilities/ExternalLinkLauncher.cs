using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Text_Grab.Controls;
using Text_Grab.Properties;

namespace Text_Grab.Utilities;

internal static class ExternalLinkLauncher
{
    internal static Task<bool> OpenAsync(string? target)
    {
        return OpenAsync(target, info => Process.Start(info), ShowErrorAsync, ConfirmAsync);
    }

    internal static async Task<bool> OpenAsync(
        string? target,
        Action<ProcessStartInfo> startProcess,
        Func<string, Task> showErrorAsync,
        Func<Uri, Task<bool>> confirmAsync)
    {
        if (!ExternalLinkUtilities.TryCreate(target, out Uri? uri))
        {
            Debug.WriteLine("External link blocked: the target is not a supported HTTP, HTTPS, mailto, or tel URI.");
            await showErrorAsync("不支援此連結。僅能開啟 HTTP、HTTPS、含收件者及選填主旨／內文的電子郵件連結，以及可含分機號碼的電話號碼。");
            return false;
        }

        if (ExternalLinkUtilities.RequiresConfirmation(uri) && !await confirmAsync(uri))
            return false;

        try
        {
            startProcess(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Debug.WriteLine($"Opening an external link failed ({ex.GetType().Name}, HRESULT: 0x{ex.HResult:X8}).");
            await showErrorAsync("無法使用預設應用程式開啟此連結。");
            return false;
        }
    }

    internal static MenuItem? CreateMenuItem(string? target, Func<string, Task<bool>>? openAsync = null)
    {
        if (!ExternalLinkUtilities.TryCreate(target, out Uri? uri))
            return null;

        string host = uri.HostNameType == UriHostNameType.IPv6 ? $"[{uri.IdnHost}]" : uri.IdnHost;
        string port = uri.IsDefaultPort ? string.Empty : $":{uri.Port}";
        string absoluteUri = uri.AbsoluteUri;
        string label = uri.Scheme switch
        {
            "mailto" => $"傳送郵件：{Uri.UnescapeDataString(absoluteUri["mailto:".Length..].Split('?')[0])}",
            "tel" => $"撥打電話：{uri.AbsolutePath}",
            _ => $"前往：{uri.Scheme}://{host}{port}"
        };
        MenuItem menuItem = new()
        {
            Header = new TextBlock { Text = label },
            ToolTip = absoluteUri
        };
        AutomationProperties.SetName(menuItem, label);

        openAsync ??= OpenAsync;
        menuItem.Click += async (_, _) => await openAsync(absoluteUri);
        return menuItem;
    }

    internal static Wpf.Ui.Controls.MessageBox CreateConfirmationDialog(Uri uri)
    {
        bool isEmail = uri.Scheme == Uri.UriSchemeMailto;
        Wpf.Ui.Controls.MessageBox dialog = new()
        {
            Title = isEmail ? "要開啟電子郵件連結嗎？" : "要開啟電話連結嗎？",
            Content = new ExternalLinkConfirmationContent(uri),
            PrimaryButtonText = isEmail ? "開啟電子郵件應用程式" : "開啟通話應用程式",
            CloseButtonText = "取消",
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        if (Application.Current?.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive) is Window owner)
            dialog.Owner = owner;

        return dialog;
    }

    private static Task<bool> ConfirmAsync(Uri uri)
    {
        return ConfirmAsync(uri, AppUtilities.TextGrabSettings, ShowConfirmationAsync);
    }

    internal static async Task<bool> ConfirmAsync(
        Uri uri,
        Settings settings,
        Func<Uri, Task<(bool Approved, bool DontWarnAgain)>> showConfirmationAsync)
    {
        bool shouldWarn = uri.Scheme switch
        {
            "mailto" => settings.WarnBeforeOpeningEmailLinks,
            "tel" => settings.WarnBeforeOpeningTelephoneLinks,
            _ => false
        };
        if (!shouldWarn)
            return true;

        (bool approved, bool dontWarnAgain) = await showConfirmationAsync(uri);
        if (!approved)
            return false;

        if (dontWarnAgain)
        {
            if (uri.Scheme == Uri.UriSchemeMailto)
                settings.WarnBeforeOpeningEmailLinks = false;
            else
                settings.WarnBeforeOpeningTelephoneLinks = false;

            settings.Save();
        }

        return true;
    }

    private static async Task<(bool Approved, bool DontWarnAgain)> ShowConfirmationAsync(Uri uri)
    {
        Wpf.Ui.Controls.MessageBox dialog = CreateConfirmationDialog(uri);
        Wpf.Ui.Controls.MessageBoxResult result = await dialog.ShowDialogAsync();
        ExternalLinkConfirmationContent content = (ExternalLinkConfirmationContent)dialog.Content;
        return (result == Wpf.Ui.Controls.MessageBoxResult.Primary, content.DontWarnAgain);
    }

    private static async Task ShowErrorAsync(string message)
    {
        await new Wpf.Ui.Controls.MessageBox
        {
            Title = "無法開啟連結",
            Content = message,
            CloseButtonText = "確定"
        }.ShowDialogAsync();
    }
}
