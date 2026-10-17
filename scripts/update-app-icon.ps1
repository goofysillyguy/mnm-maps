param([string]$SourcePath)

$ErrorActionPreference = 'Stop'
$iconProjectRoot = Split-Path -Parent $PSScriptRoot
$iconAssetDirectory = Join-Path $iconProjectRoot 'src\MnMMaps.App\Assets'
if (-not $SourcePath) { $SourcePath = Join-Path $iconAssetDirectory 'MnMMaps.png' }
$iconOutputPath = Join-Path $iconAssetDirectory 'MnMMaps.ico'

Add-Type -AssemblyName System.Drawing
$iconSource = [System.Drawing.Bitmap]::new((Resolve-Path -LiteralPath $SourcePath).Path)
$iconFrames = [System.Collections.Generic.List[byte[]]]::new()
$iconSizes = @(16, 20, 24, 32, 40, 48, 64, 96, 128, 256)
try {
    foreach ($iconSize in $iconSizes) {
        $iconBitmap = [System.Drawing.Bitmap]::new($iconSize, $iconSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $iconGraphics = [System.Drawing.Graphics]::FromImage($iconBitmap)
        $iconImageAttributes = [System.Drawing.Imaging.ImageAttributes]::new()
        $iconFrameStream = [System.IO.MemoryStream]::new()
        try {
            $iconGraphics.Clear([System.Drawing.Color]::Transparent)
            $iconGraphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $iconGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $iconGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $iconImageAttributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
            $iconScale = [Math]::Min($iconSize / $iconSource.Width, $iconSize / $iconSource.Height)
            $iconWidth = [Math]::Max(1, [int][Math]::Round($iconSource.Width * $iconScale))
            $iconHeight = [Math]::Max(1, [int][Math]::Round($iconSource.Height * $iconScale))
            $iconRectangle = [System.Drawing.Rectangle]::new([int](($iconSize - $iconWidth) / 2), [int](($iconSize - $iconHeight) / 2), $iconWidth, $iconHeight)
            $iconGraphics.DrawImage($iconSource, $iconRectangle, 0, 0, $iconSource.Width, $iconSource.Height, [System.Drawing.GraphicsUnit]::Pixel, $iconImageAttributes)
            $iconBitmap.Save($iconFrameStream, [System.Drawing.Imaging.ImageFormat]::Png)
            $iconFrames.Add($iconFrameStream.ToArray())
        }
        finally {
            $iconFrameStream.Dispose()
            $iconImageAttributes.Dispose()
            $iconGraphics.Dispose()
            $iconBitmap.Dispose()
        }
    }
}
finally { $iconSource.Dispose() }

# Modern Windows ICO files support PNG image data while retaining full alpha.
$iconFileStream = [System.IO.File]::Create($iconOutputPath)
$iconWriter = [System.IO.BinaryWriter]::new($iconFileStream)
try {
    $iconWriter.Write([uint16]0)
    $iconWriter.Write([uint16]1)
    $iconWriter.Write([uint16]$iconSizes.Count)
    $iconOffset = 6 + (16 * $iconSizes.Count)
    for ($iconIndex = 0; $iconIndex -lt $iconSizes.Count; $iconIndex++) {
        $iconDimension = if ($iconSizes[$iconIndex] -eq 256) { 0 } else { $iconSizes[$iconIndex] }
        $iconWriter.Write([byte]$iconDimension)
        $iconWriter.Write([byte]$iconDimension)
        $iconWriter.Write([byte]0)
        $iconWriter.Write([byte]0)
        $iconWriter.Write([uint16]1)
        $iconWriter.Write([uint16]32)
        $iconWriter.Write([uint32]$iconFrames[$iconIndex].Length)
        $iconWriter.Write([uint32]$iconOffset)
        $iconOffset += $iconFrames[$iconIndex].Length
    }
    foreach ($iconFrame in $iconFrames) { $iconWriter.Write([byte[]]$iconFrame) }
}
finally {
    $iconWriter.Dispose()
    $iconFileStream.Dispose()
}

Write-Output ('Application icon: ' + $iconOutputPath)
Write-Output ('Sizes: ' + ($iconSizes -join ', '))
