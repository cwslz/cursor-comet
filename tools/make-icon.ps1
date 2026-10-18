$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$assets = Join-Path $root 'assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null

function New-CometBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.Clear([System.Drawing.Color]::Transparent)
        [single]$s = $size / 32.0

        # 尾巴
        $tailPath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $tailPath.AddArc([single](2 * $s), [single](4 * $s), [single](22 * $s), [single](22 * $s), [single]120, [single]110)
        $pen = New-Object System.Drawing.Pen -ArgumentList ([System.Drawing.Color]::FromArgb(140, 90, 205, 255)), ([single](3.4 * $s))
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawPath($pen, $tailPath)
        $pen.Dispose()
        $tailPath.Dispose()

        # 头部光晕
        $headPath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $headPath.AddEllipse([single](10 * $s), [single](10 * $s), [single](16 * $s), [single](16 * $s))
        $pgb = New-Object System.Drawing.Drawing2D.PathGradientBrush -ArgumentList $headPath
        $pgb.CenterColor = [System.Drawing.Color]::White
        $pgb.SurroundColors = [System.Drawing.Color[]]@([System.Drawing.Color]::FromArgb(255, 60, 190, 255))
        $g.FillPath($pgb, $headPath)
        $pgb.Dispose()
        $headPath.Dispose()

        # 高光
        $dot = New-Object System.Drawing.SolidBrush -ArgumentList ([System.Drawing.Color]::FromArgb(235, 255, 255, 255))
        $g.FillEllipse($dot, [single](14.5 * $s), [single](14.5 * $s), [single](7 * $s), [single](7 * $s))
        $dot.Dispose()
    }
    finally {
        $g.Dispose()
    }
    return $bmp
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$pngs = @()
foreach ($size in $sizes) {
    $bmp = New-CometBitmap $size
    $ms = New-Object System.IO.MemoryStream
    try {
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngs += , $ms.ToArray()
    }
    finally {
        $ms.Dispose()
        $bmp.Dispose()
    }
}

$out = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter -ArgumentList $out
try {
    $writer.Write([UInt16]0)          # reserved
    $writer.Write([UInt16]1)          # type = icon
    $writer.Write([UInt16]$sizes.Count)

    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
        $data = $pngs[$i]
        $writer.Write([Byte]$dim)
        $writer.Write([Byte]$dim)
        $writer.Write([Byte]0)        # color count
        $writer.Write([Byte]0)        # reserved
        $writer.Write([UInt16]1)      # planes
        $writer.Write([UInt16]32)     # bit depth
        $writer.Write([UInt32]$data.Length)
        $writer.Write([UInt32]$offset)
        $offset += $data.Length
    }

    foreach ($data in $pngs) {
        $writer.Write($data)
    }

    [System.IO.File]::WriteAllBytes((Join-Path $assets 'app.ico'), $out.ToArray())
}
finally {
    $writer.Dispose()
    $out.Dispose()
}

Write-Host ('图标已生成: ' + (Join-Path $assets 'app.ico'))
