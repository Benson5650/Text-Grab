# Local x64 Windows AI build

This fork is based on v4.16.0. It permits x64 and ARM64 to reach the Windows AI
readiness API. Package identity, Windows version and the API's own hardware/model
checks still apply. A supported architecture alone does not guarantee availability.

The package uses `Benson5650.TextGrab` / `CN=Benson5650.TextGrab`, separate from the
official Store package. No Microsoft language-model LAF token is supplied.

## Build and install for this user

With Visual Studio 2026, DesktopBridge, the Windows SDK and .NET 10 installed:

```powershell
.\build-local-msix.ps1 -Deploy
```

Developer Mode must already be enabled for development registration. The script
does not change that setting or import a signing certificate. It produces an
unsigned MSIX under `artifacts/packages` and registers its build layout. Keep the
checkout/build layout in place while using the installed development package.
The unsigned MSIX is not a generally distributable installer.

## Diagnose without opening a window or touching the clipboard

Run the packaged executable through `Invoke-CommandInDesktopPackage`:

```powershell
$package = Get-AppxPackage Benson5650.TextGrab
$exe = Join-Path $package.InstallLocation 'Text-Grab\Text-Grab.exe'
Invoke-CommandInDesktopPackage -PackageFamilyName $package.PackageFamilyName `
    -AppId App -Command $exe -PreventBreakaway `
    -Args '--diagnose-windows-ai "C:\temp\windows-ai.json"'
```

The report directory must exist. The process writes JSON and exits asynchronously;
wait for the report before reading it. Add a quoted image path after the report
path to prepare the model if necessary and perform real Windows AI OCR. This can
download model components. Without an image, the command only queries readiness.
Exit codes: 0 = query/OCR completed, 1 = failure, 2 = API reports unsupported.
The report distinguishes `NotReady` from `Ready` and records API exceptions/HRESULTs.
Normal settings and history are not modified by diagnostic mode.

To uninstall this development package:

```powershell
Get-AppxPackage Benson5650.TextGrab | Remove-AppxPackage
```
