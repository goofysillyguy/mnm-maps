param([switch]$FrameworkDependent)

$ErrorActionPreference = 'Stop'
$mapTaskRoot = Split-Path -Parent $PSScriptRoot
$mapPublishDirectory = Join-Path $mapTaskRoot 'artifacts\map-overlay'
& dotnet publish (Join-Path $mapTaskRoot 'src\MnMMaps.App\MnMMaps.App.csproj') -c Release -r win-x64 `
    --self-contained ($FrameworkDependent.IsPresent -eq $false).ToString().ToLowerInvariant() `
    '-p:PublishSingleFile=true' '-p:EnableCompressionInSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:DebugType=none' '-p:DebugSymbols=false' `
    -o $mapPublishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Map overlay publish failed.' }
$mapStandaloneExe = Join-Path $mapTaskRoot 'artifacts\MnMMaps.exe'
try {
    Copy-Item -LiteralPath (Join-Path $mapPublishDirectory 'MnMMaps.exe') -Destination $mapStandaloneExe -Force
}
catch [System.IO.IOException] {
    $mapStandaloneExe = Join-Path $mapTaskRoot 'artifacts\MnMMaps-updated.exe'
    Copy-Item -LiteralPath (Join-Path $mapPublishDirectory 'MnMMaps.exe') -Destination $mapStandaloneExe -Force
    Write-Output 'The previous EXE is in use. Quit it and launch the updated copy.'
}
Write-Output "Map overlay: $mapStandaloneExe"
