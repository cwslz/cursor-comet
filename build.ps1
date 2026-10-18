$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    throw "找不到 .NET Framework C# 编译器：$csc"
}

$srcDir = Join-Path $root 'src'
$assets = Join-Path $root 'assets'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$sources = @(Get-ChildItem -Path $srcDir -Filter *.cs | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) {
    throw "src 目录里没有 .cs 文件。"
}

$exePath = Join-Path $dist '鼠标拖尾.exe'

$cscArgs = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/codepage:65001',
    '/utf8output',
    '/reference:System.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    "/out:$exePath"
)

$icon = Join-Path $assets 'app.ico'
if (Test-Path $icon) {
    $cscArgs += "/win32icon:$icon"
}
$manifest = Join-Path $assets 'app.manifest'
if (Test-Path $manifest) {
    $cscArgs += "/win32manifest:$manifest"
}

$cscArgs += $sources

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "编译失败，退出码 $LASTEXITCODE"
}

# 额外输出一份 ASCII 文件名，方便 CI/Release 使用，避免中文路径在 runner 上出问题。
$artifact = Join-Path $dist 'cursor-comet.exe'
Copy-Item $exePath $artifact -Force

Write-Host ''
Write-Host "编译完成：$exePath"
Write-Host "发布文件：$artifact"
