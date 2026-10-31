param([switch]$Deploy)

$ErrorActionPreference = 'Stop'
$repoRoot = (& git -C $PSScriptRoot rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Run this script from a Git checkout.' }
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = & $vswhere -latest -products '*' -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'Visual Studio with DesktopBridge packaging tools is required.' }
$project = Join-Path $repoRoot 'Text-Grab-Package\Text-Grab-Package.wapproj'
$packageDir = Join-Path $repoRoot 'artifacts\packages\'
& $msbuild $project /restore /nologo /v:minimal `
    /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 `
    /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly `
    /p:AppxPackageSigningEnabled=false "/p:AppxPackageDir=$packageDir"
if ($LASTEXITCODE -ne 0) { throw "MSIX build failed ($LASTEXITCODE)." }
if ($Deploy) {
    # Git resolves this checkout's junction to a physical path. AppX registration rejects
    # the OneDrive junction path with ERROR_CANT_TRAVERSE_UNTRUSTED_MOUNT_POINT.
    Add-AppxPackage -Register (Join-Path $repoRoot 'Text-Grab-Package\bin\x64\Release\AppxManifest.xml')
    Get-AppxPackage Benson5650.TextGrab | Select-Object Name,PackageFullName,InstallLocation,Status
}
Write-Host "Unsigned MSIX output: $packageDir"
