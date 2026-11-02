<#
    Dual Monitor Tools 2.13 - 一键构建脚本
    用法：
        .\build.ps1                  # 还原 + 构建全部三个解决方案（Debug）
        .\build.ps1 -Configuration Release
        .\build.ps1 -Test            # 构建后跑 NUnit 单元测试
        .\build.ps1 -Clean
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug',
    [switch]$Test,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot

# ---- 定位 MSBuild ----
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) { throw "未找到 vswhere.exe，请安装 Visual Studio 2019 或更高版本。" }
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild `
                       -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw "未找到 MSBuild.exe，请在 VS Installer 中勾选「.NET 桌面开发」工作负载。" }
Write-Host "MSBuild : $msbuild" -ForegroundColor Cyan

# ---- 定位 nuget.exe（优先用仓库内 tools 目录，其次 PATH）----
$nuget = Join-Path $Root 'tools\nuget.exe'
if (-not (Test-Path $nuget)) { $nuget = (Get-Command nuget.exe -ErrorAction SilentlyContinue).Source }
if (-not $nuget) { throw "未找到 nuget.exe。请把 nuget.exe 放到 $Root\tools\ 下，或加入 PATH。" }

$solutions = @(
    @{ Path = 'DMT\DMT.sln';                Name = 'DMT' },
    @{ Path = 'DualWallpaper\DualWallpaper.sln'; Name = 'DualWallpaper' },
    @{ Path = 'DmtWallpaper\DmtWallpaper.sln';   Name = 'DmtWallpaper' }
)

if ($Clean) {
    foreach ($s in $solutions) {
        $full = Join-Path $Root $s.Path
        Write-Host "清理 $($s.Name) ..." -ForegroundColor Yellow
        & $msbuild $full /t:Clean /p:Configuration=$Configuration /v:m /nologo | Out-Null
    }
    foreach ($d in @('DMT\DMT\bin','DMT\DMT\obj','DMT\DMT.UnitTests\bin','DMT\DMT.UnitTests\obj',
                     'DualWallpaper\DualWallpaper\bin','DualWallpaper\DualWallpaper\obj',
                     'DmtWallpaper\DmtWallpaper\bin','DmtWallpaper\DmtWallpaper\obj')) {
        $p = Join-Path $Root $d
        if (Test-Path $p) { Remove-Item -Recurse -Force $p }
    }
    Write-Host "清理完成。" -ForegroundColor Green
    return
}

# ---- 还原 NuGet 包 ----
Write-Host "`n=== NuGet 还原 ===" -ForegroundColor Cyan
& $nuget restore (Join-Path $Root 'DMT\DMT.sln') -PackagesDirectory (Join-Path $Root 'DMT\packages') -NonInteractive
if ($LASTEXITCODE -ne 0) { throw "NuGet 还原失败（$LASTEXITCODE）。若离线，请预先在 DMT\packages 放置 NUnit.3.7.1。" }

# ---- 构建 ----
$failed = @()
foreach ($s in $solutions) {
    $full = Join-Path $Root $s.Path
    Write-Host "`n=== 构建 $($s.Name) [$Configuration] ===" -ForegroundColor Cyan
    & $msbuild $full /p:Configuration=$Configuration /v:m /nologo
    if ($LASTEXITCODE -ne 0) { $failed += $s.Name }
}

if ($failed.Count -gt 0) { throw "构建失败：$($failed -join ', ')" }
Write-Host "`n全部构建成功。" -ForegroundColor Green

# ---- 单元测试 ----
if ($Test) {
    $runner = Join-Path $Root 'DMT\packages\NUnit.ConsoleRunner.3.16.3\tools\nunit3-console.exe'
    if (-not (Test-Path $runner)) {
        Write-Host "`n=== 安装 NUnit 控制台运行器 ===" -ForegroundColor Cyan
        & $nuget install NUnit.ConsoleRunner -Version 3.16.3 `
            -OutputDirectory (Join-Path $Root 'DMT\packages') -NonInteractive
    }
    $dll = Join-Path $Root "DMT\DMT.UnitTests\bin\$Configuration\DMT.UnitTests.dll"
    Write-Host "`n=== NUnit 测试 $dll ===" -ForegroundColor Cyan
    & $runner $dll --noresult
    if ($LASTEXITCODE -ne 0) { throw "单元测试失败（$LASTEXITCODE）。" }
}

# ---- 产物汇总 ----
Write-Host "`n=== 构建产物 ===" -ForegroundColor Cyan
foreach ($rel in @("DMT\DMT\bin\$Configuration\DMT.exe",
                   "DualWallpaper\DualWallpaper\bin\$Configuration\DualWallpaper.exe",
                   "DmtWallpaper\DmtWallpaper\bin\$Configuration\DmtWallpaper.exe")) {
    $p = Join-Path $Root $rel
    if (Test-Path $p) {
        "{0,10}  {1}" -f (Get-Item $p).Length, $rel
    } else {
        "   未生成   $rel"
    }
}
