using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace Text_Grab.Utilities;

internal static class WindowsAiDiagnostics
{
    internal static async Task<int> WriteReportAsync(string reportPath, string? imagePath)
    {
        var report = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.Now,
            ["architecture"] = RuntimeInformation.ProcessArchitecture.ToString(),
            ["osVersion"] = Environment.OSVersion.VersionString,
            ["packaged"] = PackageIdentity.IsPackaged()
        };
        int exitCode = 0;
        try
        {
            if (PackageIdentity.IsPackaged())
                report["packageFullName"] = Windows.ApplicationModel.Package.Current.Id.FullName;

            // Call the API directly so an application-side prerequisite cannot mask its result.
            AIFeatureReadyState state = TextRecognizer.GetReadyState();
            report["readyState"] = state.ToString();
            report["availableInTextGrab"] = WindowsAiUtilities.CanDeviceUseWinAI();
            if (state == AIFeatureReadyState.NotSupportedOnCurrentSystem)
                exitCode = 2;
            else if (imagePath is not null)
            {
                if (state == AIFeatureReadyState.NotReady)
                {
                    var ready = await TextRecognizer.EnsureReadyAsync();
                    report["ensureReadyStatus"] = ready.Status.ToString();
                    if (ready.Status != AIFeatureReadyResultState.Success)
                        throw ready.ExtendedError ?? new InvalidOperationException("Model preparation failed.");
                }
                report["readyStateAfterPreparation"] = TextRecognizer.GetReadyState().ToString();
                report["recognizedText"] = await WindowsAiUtilities.GetTextWithWinAI(Path.GetFullPath(imagePath));
            }
        }
        catch (Exception ex)
        {
            report["error"] = ex.ToString();
            report["hresult"] = $"0x{ex.HResult:X8}";
            exitCode = 1;
        }
        try
        {
            File.WriteAllText(Path.GetFullPath(reportPath), JsonSerializer.Serialize(report,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            return 1;
        }
        return exitCode;
    }
}
