using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Text_Grab.Controls;

public partial class ExternalLinkConfirmationContent : UserControl
{
    public bool DontWarnAgain => DontWarnAgainCheckBox.IsChecked is true;

    public ExternalLinkConfirmationContent(Uri uri)
    {
        InitializeComponent();
        bool isEmail = uri.Scheme == Uri.UriSchemeMailto;
        ActionDescription.Text = isEmail
            ? "這會開啟電子郵件應用程式。寄出前請確認所有收件者與訊息內容。"
            : "這會開啟通話應用程式，並可能開始通話。可能產生費用。";
        DontWarnAgainCheckBox.Content = isEmail
            ? "不再提醒我電子郵件連結"
            : "不再提醒我電話連結";
        TargetTextBox.Text = Uri.UnescapeDataString(uri.AbsoluteUri);
        TargetTextBox.ToolTip = uri.AbsoluteUri;
    }

    private void WarningButton_Click(object sender, RoutedEventArgs e)
    {
        WarningPopup.IsOpen = !WarningPopup.IsOpen;
    }

    private void Content_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && WarningPopup.IsOpen)
        {
            WarningPopup.IsOpen = false;
            e.Handled = true;
        }
    }

    private void Content_Unloaded(object sender, RoutedEventArgs e)
    {
        WarningPopup.IsOpen = false;
    }
}
