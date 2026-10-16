param([string]$Executable)

$ErrorActionPreference = 'Stop'
$mapCheckRoot = Split-Path -Parent $PSScriptRoot
if (-not $Executable) { $Executable = Join-Path $mapCheckRoot 'artifacts\release\MnMMaps.exe' }
$Executable = (Resolve-Path -LiteralPath $Executable).Path
$mapCheckOutput = Join-Path $mapCheckRoot ('artifacts\package-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $mapCheckOutput -Force | Out-Null
$mapCheckImagePath = Join-Path $mapCheckOutput 'fixture.png'
Add-Type -AssemblyName System.Drawing
$mapCheckBitmap = New-Object System.Drawing.Bitmap(1000, 800)
$mapCheckGraphics = [System.Drawing.Graphics]::FromImage($mapCheckBitmap)
$mapCheckPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(74, 94, 75), 2)
try {
    $mapCheckGraphics.Clear([System.Drawing.Color]::FromArgb(228, 221, 196))
    for ($mapGridPosition = 0; $mapGridPosition -lt 1000; $mapGridPosition += 80) {
        $mapCheckGraphics.DrawLine($mapCheckPen, $mapGridPosition, 0, $mapGridPosition, 800)
        $mapCheckGraphics.DrawLine($mapCheckPen, 0, $mapGridPosition, 1000, $mapGridPosition)
    }
    $mapCheckBitmap.Save($mapCheckImagePath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally { $mapCheckPen.Dispose(); $mapCheckGraphics.Dispose(); $mapCheckBitmap.Dispose() }
$mapCheckArguments = @('--smoke-test', ('"' + $mapCheckOutput + '"'), ('"' + $mapCheckImagePath + '"'))
$mapCheckProcess = Start-Process -FilePath $Executable -ArgumentList $mapCheckArguments -WindowStyle Hidden -Wait -PassThru
if ($mapCheckProcess.ExitCode -ne 0) {
    $mapCheckError = Join-Path $mapCheckOutput 'error.txt'
    if (Test-Path -LiteralPath $mapCheckError) { Get-Content -LiteralPath $mapCheckError }
    throw ('The packaged app check failed with exit code ' + $mapCheckProcess.ExitCode)
}
$mapCheckResult = Join-Path $mapCheckOutput 'result.txt'
if (-not (Test-Path -LiteralPath $mapCheckResult)) { throw 'The packaged app did not write its result.' }
Get-Content -LiteralPath $mapCheckResult
Write-Output ('Check output: ' + $mapCheckOutput)
