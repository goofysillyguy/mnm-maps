param(
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'Programs\MnMMaps'),
    [string]$ShortcutDirectory = [Environment]::GetFolderPath('Programs')
)

$ErrorActionPreference = 'Stop'
$mapProgramSource = Join-Path $PSScriptRoot 'MnMMaps.exe'
if (-not (Test-Path -LiteralPath $mapProgramSource -PathType Leaf)) {
    throw 'MnMMaps.exe is missing. Extract the Windows release ZIP, then run install.bat from that folder.'
}
$mapInstallRoot = [System.IO.Path]::GetFullPath($InstallDirectory)
$mapShortcutRoot = [System.IO.Path]::GetFullPath($ShortcutDirectory)
New-Item -ItemType Directory -Path $mapInstallRoot -Force | Out-Null
New-Item -ItemType Directory -Path $mapShortcutRoot -Force | Out-Null
$mapInstalledFiles = @('MnMMaps.exe','README.md','LICENSE','NOTICE.md','install.bat','install.ps1','uninstall.bat','uninstall.ps1')
foreach ($mapInstalledFile in $mapInstalledFiles) {
    $mapInstallSourceFile = Join-Path $PSScriptRoot $mapInstalledFile
    if (-not (Test-Path -LiteralPath $mapInstallSourceFile -PathType Leaf)) { continue }
    $mapInstallDestinationFile = Join-Path $mapInstallRoot $mapInstalledFile
    if ([System.IO.Path]::GetFullPath($mapInstallSourceFile) -eq $mapInstallDestinationFile) { continue }
    try { Copy-Item -LiteralPath $mapInstallSourceFile -Destination $mapInstallDestinationFile -Force }
    catch { throw 'Could not replace a program file. Quit MnM Maps using its X button or tray menu and run the installer again.' }
}
$mapDocumentationSource = Join-Path $PSScriptRoot 'docs'
if (Test-Path -LiteralPath $mapDocumentationSource) {
    $mapDocumentationTarget = Join-Path $mapInstallRoot 'docs'
    New-Item -ItemType Directory -Path $mapDocumentationTarget -Force | Out-Null
    foreach ($mapDocumentName in @('INSTALLATION.md','MAPS.md')) {
        $mapDocumentSource = Join-Path $mapDocumentationSource $mapDocumentName
        $mapDocumentDestination = Join-Path $mapDocumentationTarget $mapDocumentName
        if ((Test-Path -LiteralPath $mapDocumentSource) -and [System.IO.Path]::GetFullPath($mapDocumentSource) -ne $mapDocumentDestination) {
            Copy-Item -LiteralPath $mapDocumentSource -Destination $mapDocumentDestination -Force
        }
    }
}
$mapShell = New-Object -ComObject WScript.Shell
$mapShortcut = $mapShell.CreateShortcut((Join-Path $mapShortcutRoot 'MnM Maps.lnk'))
$mapShortcut.TargetPath = Join-Path $mapInstallRoot 'MnMMaps.exe'
$mapShortcut.WorkingDirectory = $mapInstallRoot
$mapShortcut.Description = 'Manual map overlay for Monsters and Memories'
$mapShortcut.Save()
Write-Output ('Installed MnM Maps to ' + $mapInstallRoot)
Write-Output 'Open MnM Maps from your Start Menu.'
Write-Output 'Ctrl+Shift+M shows or hides the map; Ctrl+Shift+L toggles mouse passthrough.'
Write-Output 'Your pins and settings are kept in %LOCALAPPDATA%\MnMMaps.'
