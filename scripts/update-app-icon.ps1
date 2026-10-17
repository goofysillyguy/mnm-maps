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
            if ($iconSize -eq 256) {
                $iconBitmap.Save($iconFrameStream, [System.Drawing.Imaging.ImageFormat]::Png)
            }
            else {
                # Small native icon consumers expect a DIB header, bottom-up BGRA
                # pixels, and a DWORD-aligned transparency mask. Keep PNG for 256px.
                $iconFrameWriter = [System.IO.BinaryWriter]::new($iconFrameStream, [System.Text.Encoding]::UTF8, $true)
                try {
                    $iconMaskStride = [int]([Math]::Ceiling($iconSize / 32.0) * 4)
                    $iconPixelLength = $iconSize * $iconSize * 4
                    $iconFrameWriter.Write([uint32]40)
                    $iconFrameWriter.Write([int32]$iconSize)
                    $iconFrameWriter.Write([int32]($iconSize * 2))
                    $iconFrameWriter.Write([uint16]1)
                    $iconFrameWriter.Write([uint16]32)
                    $iconFrameWriter.Write([uint32]0)
                    $iconFrameWriter.Write([uint32]($iconPixelLength + $iconMaskStride * $iconSize))
                    $iconFrameWriter.Write([int32]0)
                    $iconFrameWriter.Write([int32]0)
                    $iconFrameWriter.Write([uint32]0)
                    $iconFrameWriter.Write([uint32]0)
                    $iconBitmapBounds = [System.Drawing.Rectangle]::new(0, 0, $iconSize, $iconSize)
                    $iconBitmapData = $iconBitmap.LockBits($iconBitmapBounds, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                    try {
                        $iconPixelBytes = [byte[]]::new($iconBitmapData.Stride * $iconSize)
                        [System.Runtime.InteropServices.Marshal]::Copy($iconBitmapData.Scan0, $iconPixelBytes, 0, $iconPixelBytes.Length)
                        $iconMask = [byte[]]::new($iconMaskStride * $iconSize)
                        for ($iconRow = $iconSize - 1; $iconRow -ge 0; $iconRow--) {
                            $iconRowOffset = $iconRow * $iconBitmapData.Stride
                            $iconFrameWriter.Write($iconPixelBytes, $iconRowOffset, $iconSize * 4)
                            for ($iconColumn = 0; $iconColumn -lt $iconSize; $iconColumn++) {
                                if ($iconPixelBytes[$iconRowOffset + $iconColumn * 4 + 3] -eq 0) {
                                    $iconMaskOffset = ($iconSize - 1 - $iconRow) * $iconMaskStride + [int][Math]::Floor($iconColumn / 8.0)
                                    $iconMask[$iconMaskOffset] = [byte]($iconMask[$iconMaskOffset] -bor (128 -shr ($iconColumn % 8)))
                                }
                            }
                        }
                        $iconFrameWriter.Write($iconMask)
                    }
                    finally { $iconBitmap.UnlockBits($iconBitmapData) }
                }
                finally { $iconFrameWriter.Dispose() }
            }
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

# The directory indexes native DIB frames plus a compressed 256px PNG frame.
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
