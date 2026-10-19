$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    throw "找不到 .NET Framework C# 编译器：$csc"
}

$srcDir = Join-Path $root 'src'
$testsDir = Join-Path $root 'tests'
$dist = Join-Path $root 'dist'
$versionInfo = Join-Path $srcDir 'VersionInfo.cs'
if (-not (Test-Path $versionInfo)) {
    throw "缺少 src\VersionInfo.cs，请先运行 .\build.ps1 生成。"
}
New-Item -ItemType Directory -Force -Path $dist | Out-Null

$sources = @(Get-ChildItem -Path $srcDir -Filter *.cs | ForEach-Object { $_.FullName })
$testSource = Join-Path $testsDir 'Tests.cs'
if (-not (Test-Path $testSource)) {
    throw "缺少 tests\Tests.cs。"
}

$testExe = Join-Path $dist 'tests.exe'
$args = @(
    '/nologo',
    '/target:exe',
    '/platform:anycpu',
    '/optimize+',
    '/codepage:65001',
    '/utf8output',
    '/main:CursorComet.Tests.TestProgram',
    "/out:$testExe",
    '/reference:System.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll'
)
$args += $sources
$args += $testSource

& $csc $args
if ($LASTEXITCODE -ne 0) {
    throw "测试编译失败，退出码 $LASTEXITCODE"
}

& $testExe
if ($LASTEXITCODE -ne 0) {
    throw "测试失败，退出码 $LASTEXITCODE"
}

Write-Host "测试通过：$testExe"
