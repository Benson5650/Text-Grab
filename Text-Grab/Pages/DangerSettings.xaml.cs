using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Text_Grab.Properties;
using Text_Grab.Services;
using Text_Grab.Utilities;

namespace Text_Grab.Pages;

/// <summary>
/// Interaction logic for DangerSettings.xaml
/// </summary>
public partial class DangerSettings : System.Windows.Controls.Page
{
    private readonly Settings DefaultSettings = AppUtilities.TextGrabSettings;
    private bool _loadingDangerSettings;

    public DangerSettings()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _loadingDangerSettings = true;
        OverrideArchCheckWinAI.IsChecked = DefaultSettings.OverrideAiArchCheck;
        EnableFileBackedManagedSettingsToggle.IsChecked = DefaultSettings.EnableFileBackedManagedSettings;
        EmailLinkWarningToggle.IsChecked = DefaultSettings.WarnBeforeOpeningEmailLinks;
        TelephoneLinkWarningToggle.IsChecked = DefaultSettings.WarnBeforeOpeningTelephoneLinks;
        _loadingDangerSettings = false;
    }

    private void ExternalLinkWarningToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingDangerSettings)
            return;

        if (sender == EmailLinkWarningToggle)
            DefaultSettings.WarnBeforeOpeningEmailLinks = EmailLinkWarningToggle.IsChecked is true;
        else if (sender == TelephoneLinkWarningToggle)
            DefaultSettings.WarnBeforeOpeningTelephoneLinks = TelephoneLinkWarningToggle.IsChecked is true;
        else
            return;

        DefaultSettings.Save();
    }

    private async void ExportBugReportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string filePath = await DiagnosticsUtilities.SaveBugReportToFileAsync();

            Wpf.Ui.Controls.MessageBoxResult result = await new Wpf.Ui.Controls.MessageBox
            {
                Title = "已產生錯誤報告",
                Content = $"錯誤報告已儲存至：\n{filePath}\n\n要開啟檔案所在位置嗎？",
                PrimaryButtonText = "是",
                CloseButtonText = "否"
            }.ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                // Open the file location in File Explorer
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
        }
        catch (Exception ex)
        {
            await new Wpf.Ui.Controls.MessageBox
            {
                Title = "錯誤",
                Content = $"產生錯誤報告失敗：\n{ex.Message}",
                CloseButtonText = "確定"
            }.ShowDialogAsync();
        }
    }

    private async void ResetSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        Wpf.Ui.Controls.MessageBoxResult areYouSure = await new Wpf.Ui.Controls.MessageBox
        {
            Title = "將設定重設為預設值",
            Content = "確定要將所有設定重設為預設值並刪除所有歷史記錄嗎？",
            PrimaryButtonText = "是",
            CloseButtonText = "否"
        }.ShowDialogAsync();

        if (areYouSure != Wpf.Ui.Controls.MessageBoxResult.Primary)
            return;

        DefaultSettings.Reset();
        Singleton<HistoryService>.Instance.DeleteHistory();
        App.Current.Shutdown();
    }

    private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        Wpf.Ui.Controls.MessageBoxResult areYouSure = await new Wpf.Ui.Controls.MessageBox
        {
            Title = "將設定重設為預設值",
            Content = "確定要刪除所有歷史記錄嗎？",
            PrimaryButtonText = "是",
            CloseButtonText = "否"
        }.ShowDialogAsync();

        if (areYouSure != Wpf.Ui.Controls.MessageBoxResult.Primary)
            return;

        Singleton<HistoryService>.Instance.DeleteHistory();
    }

    private async void ExportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        await ExportSettingsAsync();
    }

    private async Task ExportSettingsAsync()
    {
        try
        {
            bool includeHistory = IncludeHistoryCheckBox.IsChecked ?? false;
            string filePath = await SettingsImportExportUtilities.ExportSettingsToZipAsync(includeHistory);

            Wpf.Ui.Controls.MessageBoxResult result = await new Wpf.Ui.Controls.MessageBox
            {
                Title = "匯出成功",
                Content = $"設定已成功匯出至：\n{filePath}\n\n要開啟檔案所在位置嗎？",
                PrimaryButtonText = "是",
                CloseButtonText = "否"
            }.ShowDialogAsync();

            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                // Open the file location in File Explorer
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
        }
        catch (System.Exception ex)
        {
            await new Wpf.Ui.Controls.MessageBox
            {
                Title = "匯出錯誤",
                Content = $"匯出設定失敗：\n{ex.Message}",
                CloseButtonText = "確定"
            }.ShowDialogAsync();
        }
    }

    private async void BackupSettingsHyperlink_Click(object sender, RoutedEventArgs e)
    {
        await ExportSettingsAsync();
    }

    private async void ImportSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Set default directory to Documents folder (where exports are saved)
            string documentsPath = AutomationProfile.Current?.OutputDirectory
                ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            OpenFileDialog openFileDialog = new()
            {
                Filter = "ZIP 檔案 (*.zip)|*.zip|所有檔案 (*.*)|*.*",
                Title = "選取設定匯出檔案",
                DefaultExt = ".zip",
                InitialDirectory = documentsPath
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            Wpf.Ui.Controls.MessageBoxResult confirmation = await new Wpf.Ui.Controls.MessageBox
            {
                Title = "確認匯入",
                Content = "匯入設定將覆寫目前設定。匯入後應用程式會重新啟動。\n\n要繼續嗎？",
                PrimaryButtonText = "是",
                CloseButtonText = "否"
            }.ShowDialogAsync();

            if (confirmation != Wpf.Ui.Controls.MessageBoxResult.Primary)
                return;

            await SettingsImportExportUtilities.ImportSettingsFromZipAsync(openFileDialog.FileName);

            await new Wpf.Ui.Controls.MessageBox
            {
                Title = "匯入成功",
                Content = "設定已成功匯入。請再次開啟 Text Grab，以完整套用所有設定。現在即將關閉…",
                CloseButtonText = "確定"
            }.ShowDialogAsync();

            // Shut down Text Grab
            App.Current.Shutdown();
        }
        catch (Exception ex)
        {
            await new Wpf.Ui.Controls.MessageBox
            {
                Title = "匯入錯誤",
                Content = $"匯入設定失敗：\n{ex.Message}",
                CloseButtonText = "確定"
            }.ShowDialogAsync();
        }
    }

    private void ShutdownButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private async void RetrySettingTrayButton_Click(object sender, RoutedEventArgs e)
    {
        await NotifyIconUtilities.ResetNotifyIcon();
    }

    private void OverrideArchCheckWinAI_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.ToggleSwitch ts)
            return;

        DefaultSettings.OverrideAiArchCheck = ts.IsChecked ?? false;
        DefaultSettings.Save();
    }

    private async void EnableFileBackedManagedSettingsToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (_loadingDangerSettings)
            return;

        bool isEnabled = EnableFileBackedManagedSettingsToggle.IsChecked is true;
        if (DefaultSettings.EnableFileBackedManagedSettings == isEnabled)
            return;

        DefaultSettings.EnableFileBackedManagedSettings = isEnabled;
        DefaultSettings.Save();

        string message = isEnabled
            ? "重新啟動 Text Grab 後，將優先使用實驗性檔案式設定儲存。必須重新啟動才能套用此偏好，並確保舊格式字串與檔案副本安全同步。若尚未備份設定，請先備份再使用。"
            : "重新啟動 Text Grab 後，將再次優先使用舊版設定儲存方式。";

        await new Wpf.Ui.Controls.MessageBox
        {
            Title = "需要重新啟動",
            Content = message,
            CloseButtonText = "確定"
        }.ShowDialogAsync();
    }
}
