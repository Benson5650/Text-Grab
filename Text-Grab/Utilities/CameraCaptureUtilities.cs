using Microsoft.UI;
using Microsoft.Windows.Media.Capture;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.Storage;

namespace Text_Grab.Utilities;

public static class CameraCaptureUtilities
{
    // The Microsoft.Windows.Media.Capture.CameraCaptureUI contract is only reliably brokered for
    // packaged (MSIX) apps, mirroring the same gate Text-Grab already applies to Windows AI features.
    public static bool IsCameraCaptureSupported() => AppUtilities.IsPackaged();

    public static async Task<string?> CaptureImageFromCameraAsync(Window ownerWindow)
    {
        if (!IsCameraCaptureSupported())
        {
            await new Wpf.Ui.Controls.MessageBox
            {
                Title = "Text Grab",
                Content = "攝影機擷取僅適用於 Microsoft Store 版本的 Text Grab。",
                CloseButtonText = "確定"
            }.ShowDialogAsync();
            return null;
        }

        try
        {
            nint hwnd = new WindowInteropHelper(ownerWindow).Handle;
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

            CameraCaptureUI cameraCaptureUI = new(windowId);
            cameraCaptureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Png;

            StorageFile? file = await cameraCaptureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            return file?.Path;
        }
        catch (Exception ex)
        {
            await new Wpf.Ui.Controls.MessageBox
            {
                Title = "Text Grab",
                Content = $"從攝影機擷取影像時發生錯誤。{Environment.NewLine}{ex.Message}",
                CloseButtonText = "確定"
            }.ShowDialogAsync();
            return null;
        }
    }
}
