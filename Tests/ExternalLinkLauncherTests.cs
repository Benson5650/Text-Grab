using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Text_Grab.Controls;
using Text_Grab.Utilities;

namespace Tests;

public class ExternalLinkLauncherTests
{
    [Theory]
    [InlineData(@"\\192.0.2.5\share\evil.jpg")]
    [InlineData("file://example.invalid/share/evil.jpg")]
    [InlineData(@"C:\Windows\notepad.exe")]
    [InlineData("ms-settings:display")]
    [InlineData("mailto:user@example.invalid?attach=file://example.invalid/share/file")]
    [InlineData("tel:not-a-number")]
    [InlineData("relative.md")]
    [InlineData("https://example.invalid/a\nb")]
    [InlineData(null)]
    public async Task OpenAsync_RejectsUnsupportedTargetsWithoutStartingAProcessOrConfirming(string? target)
    {
        int starts = 0;
        List<string> errors = [];

        bool opened = await ExternalLinkLauncher.OpenAsync(
            target,
            _ => starts++,
            message =>
            {
                errors.Add(message);
                return Task.CompletedTask;
            },
            UnexpectedConfirmation);

        Assert.False(opened);
        Assert.Equal(0, starts);
        Assert.Contains("不支援此連結。", Assert.Single(errors));
    }

    [Theory]
    [InlineData(" HTTPS://EXAMPLE.INVALID:443/a b?value=1#section ", "https://example.invalid/a%20b?value=1#section")]
    [InlineData("http://example.invalid:8080/", "http://example.invalid:8080/")]
    public async Task OpenAsync_StartsOnlyTheCanonicalWebUri(string target, string expected)
    {
        List<ProcessStartInfo> starts = [];
        List<string> errors = [];

        bool opened = await ExternalLinkLauncher.OpenAsync(
            target,
            starts.Add,
            message =>
            {
                errors.Add(message);
                return Task.CompletedTask;
            },
            UnexpectedConfirmation);

        Assert.True(opened);
        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(expected, start.FileName);
        Assert.True(start.UseShellExecute);
        Assert.Empty(start.Arguments);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task OpenAsync_ExpectedLaunchFailureReportsErrorWithoutRetrying(bool win32Error)
    {
        const string privateDetail = "sensitive URL query value";
        Exception exception = win32Error
            ? new Win32Exception(privateDetail)
            : new InvalidOperationException(privateDetail);
        int starts = 0;
        List<string> errors = [];

        bool opened = await ExternalLinkLauncher.OpenAsync(
            "https://example.invalid/",
            _ =>
            {
                starts++;
                throw exception;
            },
            message =>
            {
                errors.Add(message);
                return Task.CompletedTask;
            },
            UnexpectedConfirmation);

        Assert.False(opened);
        Assert.Equal(1, starts);
        Assert.Equal("無法使用預設應用程式開啟此連結。", Assert.Single(errors));
        Assert.DoesNotContain(privateDetail, errors[0]);
    }

    [Fact]
    public async Task OpenAsync_UnexpectedExceptionsAreNotSwallowed()
    {
        await Assert.ThrowsAsync<ApplicationException>(() => ExternalLinkLauncher.OpenAsync(
            "https://example.invalid/",
            _ => throw new ApplicationException(),
            _ => Task.CompletedTask,
            UnexpectedConfirmation));
    }

    [Theory]
    [InlineData(" MAILTO:user@EXAMPLE.INVALID?subject=Hello%20there ", "mailto:user@example.invalid?subject=Hello%20there")]
    [InlineData(" TEL:+15551234567;ext=23 ", "tel:+15551234567;ext=23")]
    public async Task OpenAsync_EmailAndTelephoneLinksWaitForExplicitApproval(string input, string expected)
    {
        TaskCompletionSource<bool> approval = new();
        List<Uri> confirmations = [];
        List<ProcessStartInfo> starts = [];

        Task<bool> opening = ExternalLinkLauncher.OpenAsync(
            input,
            starts.Add,
            UnexpectedError,
            uri =>
            {
                confirmations.Add(uri);
                return approval.Task;
            });

        Assert.False(opening.IsCompleted);
        Assert.Empty(starts);
        Assert.Equal(expected, Assert.Single(confirmations).AbsoluteUri);

        approval.SetResult(true);

        Assert.True(await opening);
        ProcessStartInfo start = Assert.Single(starts);
        Assert.Equal(expected, start.FileName);
        Assert.True(start.UseShellExecute);
    }

    [Theory]
    [InlineData("mailto:user@example.invalid")]
    [InlineData("tel:+15551234567")]
    public async Task OpenAsync_CancelDoesNotLaunchOrShowAnError(string input)
    {
        int starts = 0;
        int confirmations = 0;

        bool opened = await ExternalLinkLauncher.OpenAsync(
            input,
            _ => starts++,
            UnexpectedError,
            _ =>
            {
                confirmations++;
                return Task.FromResult(false);
            });

        Assert.False(opened);
        Assert.Equal(1, confirmations);
        Assert.Equal(0, starts);
    }

    [Fact]
    public async Task OpenAsync_ConfirmationIsRequiredEveryTime()
    {
        int confirmations = 0;
        int starts = 0;
        for (int i = 0; i < 2; i++)
        {
            Assert.True(await ExternalLinkLauncher.OpenAsync(
                "mailto:user@example.invalid",
                _ => starts++,
                UnexpectedError,
                _ =>
                {
                    confirmations++;
                    return Task.FromResult(true);
                }));
        }

        Assert.Equal(2, confirmations);
        Assert.Equal(2, starts);
    }

    [WpfFact]
    public void CreateMenuItem_OmitsUnsafeTargets()
    {
        string[] targets =
        [
            @"\\192.0.2.5\share\evil.jpg",
            "                              file://example.invalid/share/evil.jpg",
            @"C:\Windows\notepad.exe",
            "ms-settings:display",
            "relative.md"
        ];

        foreach (string target in targets)
            Assert.Null(ExternalLinkLauncher.CreateMenuItem(target));
    }

    [WpfFact]
    public void CreateMenuItem_ShowsTheActualHostAndKeepsTheValidatedTarget()
    {
        string target = " HTTPS://trusted.invalid@example.invalid:8443/very/long/path?value=1 ";
        List<string> opened = [];
        MenuItem menuItem = Assert.IsType<MenuItem>(ExternalLinkLauncher.CreateMenuItem(
            target,
            url =>
            {
                opened.Add(url);
                return Task.FromResult(true);
            }));

        target = @"\\192.0.2.5\share\evil.jpg";
        menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        const string canonicalUri = "https://trusted.invalid@example.invalid:8443/very/long/path?value=1";
        Assert.Equal(canonicalUri, Assert.Single(opened));
        Assert.Equal(canonicalUri, menuItem.ToolTip);
        Assert.Equal("前往：https://example.invalid:8443", Assert.IsType<TextBlock>(menuItem.Header).Text);
        Assert.Equal("前往：https://example.invalid:8443", AutomationProperties.GetName(menuItem));
    }

    [WpfFact]
    public void CreateMenuItem_DoesNotTruncateTheHostOrTreatUnderscoresAsAccessKeys()
    {
        const string target = "https://long_descriptive_name.example.invalid/path";
        MenuItem menuItem = Assert.IsType<MenuItem>(ExternalLinkLauncher.CreateMenuItem(target));

        Assert.Equal("前往：https://long_descriptive_name.example.invalid", Assert.IsType<TextBlock>(menuItem.Header).Text);
        Assert.Equal(target, menuItem.ToolTip);
    }

    [WpfFact]
    public void CreateMenuItem_DisplaysIpv6AuthorityWithBrackets()
    {
        MenuItem menuItem = Assert.IsType<MenuItem>(ExternalLinkLauncher.CreateMenuItem("http://[::1]:8080/path"));

        Assert.Equal("前往：http://[::1]:8080", Assert.IsType<TextBlock>(menuItem.Header).Text);
    }

    [WpfFact]
    public void CreateMenuItem_EmailAndTelephoneLabelsDescribeTheAction()
    {
        MenuItem emailItem = Assert.IsType<MenuItem>(ExternalLinkLauncher.CreateMenuItem("mailto:user@example.invalid?subject=Hello"));
        MenuItem phoneItem = Assert.IsType<MenuItem>(ExternalLinkLauncher.CreateMenuItem("tel:+15551234567;ext=23"));

        Assert.Equal("傳送郵件：user@example.invalid", Assert.IsType<TextBlock>(emailItem.Header).Text);
        Assert.Equal("撥打電話：+15551234567;ext=23", Assert.IsType<TextBlock>(phoneItem.Header).Text);
    }

    [WpfFact]
    public void CreateConfirmationDialog_ShowsCompleteLinkAndExplicitOpenAndCancelChoices()
    {
        foreach (string input in new[] { "mailto:user@example.invalid?subject=Hello%20there&body=Line%0D%0Atwo", "tel:+15551234567;ext=23" })
        {
            Uri uri = new(input);
            Wpf.Ui.Controls.MessageBox dialog = ExternalLinkLauncher.CreateConfirmationDialog(uri);
            try
            {
                ExternalLinkConfirmationContent content = Assert.IsType<ExternalLinkConfirmationContent>(dialog.Content);
                TextBox target = Assert.IsType<TextBox>(content.FindName("TargetTextBox"));
                CheckBox dontWarnAgain = Assert.IsType<CheckBox>(content.FindName("DontWarnAgainCheckBox"));
                bool isEmail = uri.Scheme == "mailto";

                Assert.Equal(isEmail ? "要開啟電子郵件連結嗎？" : "要開啟電話連結嗎？", dialog.Title);
                Assert.Equal(isEmail ? "開啟電子郵件應用程式" : "開啟通話應用程式", dialog.PrimaryButtonText);
                Assert.Equal("取消", dialog.CloseButtonText);
                Assert.Equal(Uri.UnescapeDataString(uri.AbsoluteUri), target.Text);
                Assert.Equal(uri.AbsoluteUri, target.ToolTip);
                Assert.True(target.IsReadOnly);
                Assert.Equal(
                    isEmail ? "不再提醒我電子郵件連結" : "不再提醒我電話連結",
                    dontWarnAgain.Content);
                Assert.False(content.DontWarnAgain);
                dontWarnAgain.IsChecked = true;
                Assert.True(content.DontWarnAgain);
            }
            finally
            {
                ((Window)dialog).Close();
            }
        }
    }

    private static Task<bool> UnexpectedConfirmation(Uri _) =>
        throw new InvalidOperationException("This link must not request confirmation.");

    private static Task UnexpectedError(string message) =>
        throw new InvalidOperationException($"Unexpected error: {message}");
}
