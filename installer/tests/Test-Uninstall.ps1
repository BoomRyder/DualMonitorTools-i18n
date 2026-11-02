<#
    验证卸载清理：静默装到临时目录 -> 制造全部运行时残留 -> 静默卸载 -> 断言全部消失。
    需要管理员权限。会临时改动 HKCU\Control Panel\Desktop 的壁纸设置，测试后复原。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SetupExe
)

$ErrorActionPreference = 'Stop'

$targetDir  = Join-Path $env:TEMP ('dmt_uninst_' + [guid]::NewGuid().ToString('N'))
$dataDir    = Join-Path $env:APPDATA 'Dual Monitor Tools'
$dmtKey     = 'HKCU:\SOFTWARE\Dual Monitor Tools'
$ctxKey     = 'HKCU:\Software\Classes\directory\Background\shell\GNE_DualMonitorTools'
$runKey     = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$desktopKey = 'HKCU:\Control Panel\Desktop'
$ssKey      = 'HKLM:\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools'
$scrPath    = Join-Path $env:WINDIR 'System32\DmtWallpaper.scr'
$scrWowPath = Join-Path $env:WINDIR 'SysWOW64\DmtWallpaper.scr'

function Assert-True($cond, $msg) {
    if (-not $cond) { throw "断言失败：$msg" }
}

function Wait-UntilGone($path, $timeoutSeconds) {
    # Inno 的卸载器会把自己复制到 %TEMP% 再重启一份，父进程可能先返回，
    # 所以不能只靠 -Wait，必须轮询到文件真的消失。
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (-not (Test-Path $path)) { return $true }
        Start-Sleep -Milliseconds 400
    }
    return (-not (Test-Path $path))
}

# --- 保存壁纸原始状态 ---
$origWallpaper      = (Get-ItemProperty $desktopKey -Name Wallpaper      -ErrorAction SilentlyContinue).Wallpaper
$origTileWallpaper  = (Get-ItemProperty $desktopKey -Name TileWallpaper  -ErrorAction SilentlyContinue).TileWallpaper
$origWallpaperStyle = (Get-ItemProperty $desktopKey -Name WallpaperStyle -ErrorAction SilentlyContinue).WallpaperStyle

$targetDir2   = $null
$foreign      = $null
$foreignIsTemp = $false

try {
    Assert-True (Test-Path $SetupExe) "找不到安装包：$SetupExe"

    # --- 基线检查：这些必须都不存在，否则测试不可信 ---
    foreach ($p in @($dmtKey, $ctxKey, $dataDir, $ssKey, $scrPath, $scrWowPath)) {
        Assert-True (-not (Test-Path $p)) "基线已被污染，先手工清理：$p"
    }

    # --- 静默安装 ---
    $p = Start-Process -FilePath $SetupExe `
        -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir" `
        -Wait -PassThru
    Assert-True ($p.ExitCode -eq 0) "安装失败，退出码 $($p.ExitCode)"
    Assert-True (Test-Path (Join-Path $targetDir 'DMT.exe')) "安装后 DMT.exe 不存在"
    Assert-True (Test-Path (Join-Path $targetDir 'zh-Hans\DMT.resources.dll')) "中文卫星程序集未安装到 zh-Hans 子目录"
    Assert-True (Test-Path $scrPath) "屏保未安装到 System32"
    # 32 位安装包写 {sys} 会被 WOW64 重定向到 SysWOW64，64 位 desk.cpl 就看不到这个屏保
    Assert-True (-not (Test-Path $scrWowPath)) "屏保漏到了 SysWOW64（WOW64 重定向未规避）"
    Assert-True (Test-Path $ssKey) "ScreenSaverInstalled 标记未写入"
    Write-Host "安装阶段 PASS" -ForegroundColor Green

    # --- 制造全部运行时残留 ---
    # 1) 界面语言 + 壁纸路径
    New-Item -Path "$dmtKey\DMT" -Force | Out-Null
    New-ItemProperty -Path "$dmtKey\DMT" -Name 'Language' -Value 'zh-Hans' -PropertyType String -Force | Out-Null
    New-ItemProperty -Path "$dmtKey\DMT" -Name 'DmtWallpaperPath' -Value "$dataDir\DmtWallpaper.bmp" -PropertyType String -Force | Out-Null

    # 2) 开机自启
    if (-not (Test-Path $runKey)) { New-Item -Path $runKey -Force | Out-Null }
    New-ItemProperty -Path $runKey -Name 'GNE_DualMonitorTools' -Value "$targetDir\DMT.exe" -PropertyType String -Force | Out-Null

    # 3) 右键菜单子树（模拟运行期生成的子命令）
    foreach ($sub in @('Options', 'MagicWord', 'AddMagicWord', 'MakePrimary', 'ChangeBrightness',
                       'ChangeContrast', 'ChangeOrientation', 'MinimizeAll', 'MoveAll',
                       'ChangeWallpaper')) {
        $k = "$ctxKey\$sub\command"
        New-Item -Path $k -Force | Out-Null
        New-ItemProperty -Path $k -Name '(default)' -Value "`"$targetDir\DMT.exe`" DMT:General:$sub" -PropertyType String -Force | Out-Null
    }

    # 4) 数据目录与其中的文件
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    Set-Content -Path (Join-Path $dataDir 'DmtSettings.xml') -Value '<settings><setting name="___FirstRun" value="False" /></settings>' -Encoding utf8
    Set-Content -Path (Join-Path $dataDir 'DmtMagicWords.xml') -Value '<magicwords />' -Encoding utf8
    New-Item -ItemType Directory -Path (Join-Path $dataDir 'DmtWallpaperPositions') -Force | Out-Null
    Set-Content -Path (Join-Path $dataDir 'DmtWallpaperPositions\1.dmtp') -Value 'x' -Encoding ascii
    [System.IO.File]::WriteAllBytes((Join-Path $dataDir 'DmtWallpaper.bmp'), (New-Object byte[] 1024))

    # 5) 让壁纸指向数据目录，模拟 DMT 改过壁纸
    Set-ItemProperty -Path $desktopKey -Name 'Wallpaper' -Value (Join-Path $dataDir 'DmtWallpaper.bmp')

    Write-Host "已制造全部运行时残留" -ForegroundColor Yellow

    # --- 静默卸载 ---
    $unins = Join-Path $targetDir 'unins000.exe'
    Assert-True (Test-Path $unins) "卸载程序不存在"
    $u = Start-Process -FilePath $unins `
        -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
        -Wait -PassThru
    Assert-True ($u.ExitCode -eq 0) "卸载失败，退出码 $($u.ExitCode)"

    $gone = Wait-UntilGone (Join-Path $targetDir 'DMT.exe') 90
    Assert-True $gone "卸载 90 秒后 DMT.exe 仍存在，卸载未真正完成"

    # --- 断言全部消失 ---
    Assert-True (-not (Test-Path $dmtKey))  "HKCU\SOFTWARE\Dual Monitor Tools 子树未删除"
    Assert-True (-not (Test-Path $ctxKey))  "右键菜单子树未删除（只删空键会留下子命令）"
    $runValue = (Get-ItemProperty $runKey -Name 'GNE_DualMonitorTools' -ErrorAction SilentlyContinue).GNE_DualMonitorTools
    Assert-True ($null -eq $runValue)       "开机自启值未删除"
    Assert-True (-not (Test-Path $dataDir)) "数据目录未删除"
    Assert-True (-not (Test-Path $ssKey))   "ScreenSaverInstalled 标记未删除"
    Assert-True (-not (Test-Path (Split-Path $ssKey -Parent))) "HKLM\...\WOW6432Node\GNE 空键未清理"
    Assert-True (-not (Test-Path $scrPath)) "System32 下的屏保未删除"

    $w = (Get-ItemProperty $desktopKey -Name Wallpaper -ErrorAction SilentlyContinue).Wallpaper
    Assert-True ($w -eq '') "壁纸未被清空（实际值：'$w'）"

    Write-Host "卸载阶段 PASS：全部残留已清除" -ForegroundColor Green

    # --- 阶段二：壁纸指向用户自己的图片时，卸载不得动它 ---
    Write-Host "`n=== 阶段二：非 DMT 管理的壁纸必须保留 ===" -ForegroundColor Cyan
    $targetDir2 = Join-Path $env:TEMP ('dmt_uninst2_' + [guid]::NewGuid().ToString('N'))

    $foreign = Join-Path $env:WINDIR 'Web\Wallpaper\Windows\img0.jpg'
    if (-not (Test-Path $foreign)) {
        $foreign = Join-Path $env:TEMP ('dmt_foreign_' + [guid]::NewGuid().ToString('N') + '.bmp')
        [System.IO.File]::WriteAllBytes($foreign, (New-Object byte[] 1024))
        $foreignIsTemp = $true
    }

    $p2 = Start-Process -FilePath $SetupExe `
        -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir2" `
        -Wait -PassThru
    Assert-True ($p2.ExitCode -eq 0) "阶段二安装失败，退出码 $($p2.ExitCode)"

    Set-ItemProperty -Path $desktopKey -Name 'Wallpaper' -Value $foreign

    $u2 = Start-Process -FilePath (Join-Path $targetDir2 'unins000.exe') `
        -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' `
        -Wait -PassThru
    Assert-True ($u2.ExitCode -eq 0) "阶段二卸载失败，退出码 $($u2.ExitCode)"
    Assert-True (Wait-UntilGone (Join-Path $targetDir2 'DMT.exe') 90) "阶段二卸载未真正完成"

    $w2 = (Get-ItemProperty $desktopKey -Name Wallpaper -ErrorAction SilentlyContinue).Wallpaper
    Assert-True ($w2 -eq $foreign) "卸载误清了用户自己的壁纸（期望 '$foreign'，实际 '$w2'）"

    Write-Host "阶段二 PASS：非 DMT 管理的壁纸被保留" -ForegroundColor Green
}
finally {
    # --- 复原壁纸 ---
    if ($null -ne $origWallpaper) {
        Set-ItemProperty -Path $desktopKey -Name 'Wallpaper' -Value $origWallpaper
    }
    if ($null -ne $origTileWallpaper) {
        Set-ItemProperty -Path $desktopKey -Name 'TileWallpaper' -Value $origTileWallpaper
    }
    if ($null -ne $origWallpaperStyle) {
        Set-ItemProperty -Path $desktopKey -Name 'WallpaperStyle' -Value $origWallpaperStyle
    }

    # 让桌面立刻重绘回原壁纸
    if ($null -ne $origWallpaper) {
        $sig = @'
using System;
using System.Runtime.InteropServices;
public class DmtWallpaperRestore {
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SystemParametersInfoW(int uAction, int uParam, string lpvParam, int fuWinIni);
}
'@
        if (-not ('DmtWallpaperRestore' -as [type])) {
            Add-Type -TypeDefinition $sig -ErrorAction SilentlyContinue
        }
        [DmtWallpaperRestore]::SystemParametersInfoW(20, 0, $origWallpaper, 1) | Out-Null
    }

    # --- 兜底清理 ---
    foreach ($p in @($dmtKey, $ctxKey, $dataDir, $targetDir)) {
        if ($p -and (Test-Path $p)) { Remove-Item -Path $p -Recurse -Force -ErrorAction SilentlyContinue }
    }
    if ($targetDir2 -and (Test-Path $targetDir2)) {
        Remove-Item -Path $targetDir2 -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($foreignIsTemp -and $foreign -and (Test-Path $foreign)) {
        Remove-Item -Path $foreign -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $ssKey) { Remove-Item -Path $ssKey -Recurse -Force -ErrorAction SilentlyContinue }
    $ssParent = Split-Path $ssKey -Parent
    if (Test-Path $ssParent) {
        $children = Get-ChildItem $ssParent -ErrorAction SilentlyContinue
        if (-not $children) { Remove-Item -Path $ssParent -Force -ErrorAction SilentlyContinue }
    }
    if (Test-Path $scrPath) { Remove-Item -Path $scrPath -Force -ErrorAction SilentlyContinue }
    if (Test-Path $scrWowPath) { Remove-Item -Path $scrWowPath -Force -ErrorAction SilentlyContinue }

    Write-Host "已复原壁纸与清理测试残留" -ForegroundColor Yellow
}
