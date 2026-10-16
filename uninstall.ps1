param(
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'Programs\MnMMaps'),
    [string]$ShortcutDirectory = [Environment]::GetFolderPath('Programs')
)

$ErrorActionPreference = 'Stop'
$mapUninstallRoot = [System.IO.Path]::GetFullPath($InstallDirectory)
$mapShortcutRoot = [System.IO.Path]::GetFullPath($ShortcutDirectory)
$mapKnownProgramFiles = @('MnMMaps.exe','README.md','LICENSE','NOTICE.md','install.bat','install.ps1','uninstall.bat','uninstall.ps1','docs\INSTALLATION.md','docs\MAPS.md')
foreach ($mapKnownProgramFile in $mapKnownProgramFiles) {
    $mapRemovalTarget = [System.IO.Path]::GetFullPath((Join-Path $mapUninstallRoot $mapKnownProgramFile))
    if (-not $mapRemovalTarget.StartsWith($mapUninstallRoot.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The removal path is outside the selected installation folder.'
    }
    if (Test-Path -LiteralPath $mapRemovalTarget -PathType Leaf) {
        try { Remove-Item -LiteralPath $mapRemovalTarget -Force }
        catch { throw 'A program file is still in use. Quit MnM Maps using its X button or tray menu, then run uninstall.bat again.' }
    }
}
$mapShortcutPath = Join-Path $mapShortcutRoot 'MnM Maps.lnk'
if (Test-Path -LiteralPath $mapShortcutPath -PathType Leaf) { Remove-Item -LiteralPath $mapShortcutPath -Force }
# Remove only empty directories. Personal app data and unrecognized files stay in place.
$mapDocumentsPath = Join-Path $mapUninstallRoot 'docs'
if ((Test-Path -LiteralPath $mapDocumentsPath -PathType Container) -and @(Get-ChildItem -LiteralPath $mapDocumentsPath -Force).Count -eq 0) {
    Remove-Item -LiteralPath $mapDocumentsPath
}
if ((Test-Path -LiteralPath $mapUninstallRoot -PathType Container) -and @(Get-ChildItem -LiteralPath $mapUninstallRoot -Force).Count -eq 0) {
    Remove-Item -LiteralPath $mapUninstallRoot
}
Write-Output 'MnM Maps program files and the Start Menu shortcut were removed.'
Write-Output 'Your pins, imported maps and saved settings remain in %LOCALAPPDATA%\MnMMaps.'
