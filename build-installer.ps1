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

function Set-SetupRequiresAdministrator {
    <#
        Inno 的 Setup.exe 清单一律是 asInvoker（loader 运行期用 runas 自重启提权），
        所以资源管理器不给它画 UAC 盾牌、双击也看不出来要管理员。
        用 mt.exe 之类"重写资源"会把包截断（只按 PE 镜像大小重写，后面追加的安装数据
        包全丢，2337555 -> 849920 字节），因此只能等长替换：
        asInvoker(9) 换成 requireAdministrator(20) 多的 11 字节，从 <trustInfo> 前那段
        Inno 自带的空白行里砍掉，文件大小与所有偏移保持不变。
    #>
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $s = [System.Text.Encoding]::Latin1.GetString($bytes)

    if ($s.Contains('level="requireAdministrator"')) { return }
    if (-not $s.Contains('level="asInvoker"')) {
        throw "清单里找不到 asInvoker，无法改为 requireAdministrator（Inno 版本变了？请核对 $Path）。"
    }

    $s2 = $s.Replace('level="asInvoker"', 'level="requireAdministrator"')
    $mm = ([regex]'( +)\r?\n(?=<trustInfo)').Match($s2)
    if (-not $mm.Success -or $mm.Groups[1].Length -lt 11) {
        throw "清单里没有足够的填充空白做等长替换，无法改为 requireAdministrator。"
    }

    $s3 = $s2.Remove($mm.Groups[1].Index + $mm.Groups[1].Length - 11, 11)
    if ($s3.Length -ne $s.Length) { throw "等长替换失败，放弃写回以免损坏安装包。" }

    [System.IO.File]::WriteAllBytes($Path, [System.Text.Encoding]::Latin1.GetBytes($s3))
}

# 产物名里含 ISPP 宏（{#MyAppVersion}），不去展开它，直接取刚编译出来的那个
$setupExe = Get-ChildItem $dist -Filter '*Setup.exe' | Sort-Object LastWriteTimeUtc | Select-Object -Last 1
if (-not $setupExe) { throw "在 $dist 里找不到刚编译出来的安装包。" }
Set-SetupRequiresAdministrator -Path $setupExe.FullName

Write-Host "`n=== 产物 ===" -ForegroundColor Cyan
"已把清单改为 requireAdministrator（等长替换，文件大小不变）"
Get-ChildItem $dist -Filter '*.exe' | ForEach-Object {
    "{0,10}  {1}" -f $_.Length, $_.Name
}
