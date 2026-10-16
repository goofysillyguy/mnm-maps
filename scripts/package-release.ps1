param([switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$mapPackageRoot = Split-Path -Parent $PSScriptRoot
if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'publish-maps.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'The standalone build failed.' }
}
$mapBuiltExe = Join-Path $mapPackageRoot 'artifacts\map-overlay\MnMMaps.exe'
if (-not (Test-Path -LiteralPath $mapBuiltExe)) { throw 'Build the standalone EXE before packaging.' }
$mapReleaseRoot = Join-Path $mapPackageRoot 'artifacts\release'
$mapReleaseStage = Join-Path $mapPackageRoot ('artifacts\release-stage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $mapReleaseStage 'docs') -Force | Out-Null
New-Item -ItemType Directory -Path $mapReleaseRoot -Force | Out-Null
Copy-Item -LiteralPath $mapBuiltExe -Destination (Join-Path $mapReleaseStage 'MnMMaps.exe')
Copy-Item -LiteralPath $mapBuiltExe -Destination (Join-Path $mapReleaseRoot 'MnMMaps.exe') -Force
foreach ($mapReleaseFile in @('README.md','LICENSE','NOTICE.md','install.bat','install.ps1','uninstall.bat','uninstall.ps1')) {
    Copy-Item -LiteralPath (Join-Path $mapPackageRoot $mapReleaseFile) -Destination (Join-Path $mapReleaseStage $mapReleaseFile)
}
foreach ($mapReleaseDocument in @('INSTALLATION.md','MAPS.md')) {
    Copy-Item -LiteralPath (Join-Path $mapPackageRoot ('docs\' + $mapReleaseDocument)) -Destination (Join-Path $mapReleaseStage ('docs\' + $mapReleaseDocument))
}
$mapZip = Join-Path $mapReleaseRoot 'MnMMaps-win-x64.zip'
Compress-Archive -Path (Join-Path $mapReleaseStage '*') -DestinationPath $mapZip -CompressionLevel Optimal -Force
$mapChecksumLines = foreach ($mapAssetName in @('MnMMaps.exe','MnMMaps-win-x64.zip')) {
    $mapAssetHash = (Get-FileHash -Algorithm SHA256 -LiteralPath (Join-Path $mapReleaseRoot $mapAssetName)).Hash.ToLowerInvariant()
    $mapAssetHash + '  ' + $mapAssetName
}
[System.IO.File]::WriteAllText((Join-Path $mapReleaseRoot 'SHA256SUMS.txt'), ($mapChecksumLines -join "`n") + "`n", [System.Text.Encoding]::ASCII)
Write-Output ('Release assets: ' + $mapReleaseRoot)
