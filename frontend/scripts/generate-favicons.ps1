Add-Type -AssemblyName System.Drawing

$sourcePath = Join-Path $PSScriptRoot "..\public\brand\zeropaper-logo.png"
$publicPath = Join-Path $PSScriptRoot "..\public"
$previewPath = Join-Path $PSScriptRoot "..\public\brand\_favicon-preview.png"

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path $sourcePath))
$cropRect = New-Object System.Drawing.Rectangle(450, 90, 640, 440)
$crop = $source.Clone($cropRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$crop.MakeTransparent([System.Drawing.Color]::White)

function New-IconBitmap([System.Drawing.Bitmap]$sourceBitmap, [int]$size) {
    $canvas = New-Object System.Drawing.Bitmap($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

    $padding = [Math]::Max([Math]::Round($size * 0.08), 2)
    $targetWidth = $size - ($padding * 2)
    $targetHeight = [Math]::Round($sourceBitmap.Height * ($targetWidth / $sourceBitmap.Width))

    if ($targetHeight -gt ($size - ($padding * 2))) {
        $targetHeight = $size - ($padding * 2)
        $targetWidth = [Math]::Round($sourceBitmap.Width * ($targetHeight / $sourceBitmap.Height))
    }

    $x = [Math]::Round(($size - $targetWidth) / 2)
    $y = [Math]::Round(($size - $targetHeight) / 2)
    $graphics.DrawImage($sourceBitmap, $x, $y, $targetWidth, $targetHeight)
    $graphics.Dispose()
    return $canvas
}

$sizes = @(
    @("favicon-16x16.png", 16),
    @("favicon-32x32.png", 32),
    @("favicon-48x48.png", 48),
    @("apple-touch-icon.png", 180),
    @("android-chrome-192x192.png", 192),
    @("android-chrome-512x512.png", 512),
    @("icon-512.png", 512)
)

foreach ($entry in $sizes) {
    $bitmap = New-IconBitmap -sourceBitmap $crop -size $entry[1]
    $bitmap.Save((Join-Path $publicPath $entry[0]), [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

$faviconBitmap = New-IconBitmap -sourceBitmap $crop -size 32
$icon = [System.Drawing.Icon]::FromHandle($faviconBitmap.GetHicon())
$fileStream = [System.IO.File]::Create((Join-Path $publicPath "favicon.ico"))
$icon.Save($fileStream)
$fileStream.Dispose()

$faviconBitmap.Dispose()
$icon.Dispose()
$crop.Dispose()
$source.Dispose()

if (Test-Path $previewPath) {
    Remove-Item $previewPath -Force
}
