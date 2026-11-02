<#
    Dual Monitor Tools 2.13 —— 一键构建安装包
    用法：
        .\build-installer.ps1
        .\build-installer.ps1 -SkipBuild     # 复用已有 bin\Release
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot

function Find-InnoCompiler {
    # 1) 注册表（Inno 7 把自身卸载项写在 64 位视图）
    $regPaths = @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1',
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 7_is1'
    )
    foreach ($rp in $regPaths) {
        if (Test-Path $rp) {
            $loc = (Get-ItemProperty $rp -ErrorAction SilentlyContinue).InstallLocation
            if ($loc) {
                $exe = Join-Path $loc 'ISCC.exe'
                if (Test-Path $exe) { return $exe }
            }
        }
    }

    # 2) 环境变量
    if ($env:INNO_SETUP_DIR) {
        $exe = Join-Path $env:INNO_SETUP_DIR 'ISCC.exe'
        if (Test-Path $exe) { return $exe }
    }

    # 3) 常见安装目录
    foreach ($d in @('D:\Program Files\Inno Setup 7', 'C:\Program Files (x86)\Inno Setup 7', 'C:\Program Files\Inno Setup 7')) {
        $exe = Join-Path $d 'ISCC.exe'
        if (Test-Path $exe) { return $exe }
    }

    throw "未找到 ISCC.exe。请安装 Inno Setup 7，或设置环境变量 INNO_SETUP_DIR 指向其安装目录。"
}

$iscc = Find-InnoCompiler
Write-Host "ISCC    : $iscc" -ForegroundColor Cyan

if (-not $SkipBuild) {
    Write-Host "`n=== 构建 Release 产物 ===" -ForegroundColor Cyan
    # build.ps1 失败时自己 throw，异常会直接向上冒泡（本脚本 $ErrorActionPreference=Stop），
    # 不需要额外检查退出码 —— .ps1 不会设置 $LASTEXITCODE。
    & (Join-Path $Root 'build.ps1') -Configuration Release
}

$dist = Join-Path $Root 'dist'
if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }

$iss = Join-Path $Root 'installer\DualMonitorTools.iss'
Write-Host "`n=== 编译安装包 ===" -ForegroundColor Cyan
& $iscc "/DRepoRoot=$Root" "/O$dist" $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC 编译失败（$LASTEXITCODE）。" }

Write-Host "`n=== 产物 ===" -ForegroundColor Cyan
Get-ChildItem $dist -Filter '*.exe' | ForEach-Object {
    "{0,10}  {1}" -f $_.Length, $_.Name
}
