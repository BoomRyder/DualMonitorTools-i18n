#Requires -RunAsAdministrator
<#
    验证卸载清理：静默装到临时目录 -> 制造全部运行时残留 -> 静默卸载 -> 断言全部消失。
    需要管理员权限。会临时改动 HKCU\Control Panel\Desktop 的壁纸与屏保设置，测试后复原。
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
# {autodesktop} 在提权安装下就是公共桌面
$desktopLnk = Join-Path 'C:\Users\Public\Desktop' 'Dual Monitor Tools.lnk'
function Assert-True($cond, $msg) {
    if (-not $cond) { throw "断言失败：$msg" }
}

function Find-DmtUninstallEntry {
    # 卸载器自己失败时，"程序和功能"里会留下指向空目录的条目；基线检查和兜底清理都要用它。
    foreach ($root in @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
                        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall')) {
        if (-not (Test-Path $root)) { continue }
        Get-ChildItem $root -ErrorAction SilentlyContinue | ForEach-Object {
            $dn = (Get-ItemProperty $_.PSPath -Name DisplayName -ErrorAction SilentlyContinue).DisplayName
            if ($dn -eq 'Dual Monitor Tools') { $_.PSPath }
        }
    }
}

function Invoke-Program {
    # 用 CreateProcess（UseShellExecute=$false）而不是 Start-Process：后者走
    # ShellExecute，会因 MOTW / 杀软检查弹出"打开文件 - 安全警告"对话框，
    # 静默安装就会一直卡在那里等用户点按钮。
    param([string]$Exe, [string[]]$Arguments, [int]$TimeoutSeconds = 180)
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $Exe
    $psi.Arguments = ($Arguments | ForEach-Object {
        if ($_ -match '\s') { '"' + $_ + '"' } else { $_ }
    }) -join ' '
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $proc = [System.Diagnostics.Process]::Start($psi)
    if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) {
        try { $proc.Kill() } catch { }
        throw "超时：$Exe 在 $TimeoutSeconds 秒内未退出"
    }
    return $proc.ExitCode
}

function Wait-Until([scriptblock]$Condition, $timeoutSeconds) {
    # Inno 的卸载器把自己复制到 %TEMP% 再重启一份，第一个进程退出不等于卸载
    # 完成：[Code] 的清理在重启后的那个进程里跑。断言前必须等到结果真的出现，
    # 否则测的是"还没做完"，不是"做坏了"。
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) { return $true }
        Start-Sleep -Milliseconds 300
    }
    return (& $Condition)
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

# --- 保存屏保原始状态（DMT 不写这个值，是控制面板写的） ---
$origScreenSaver = (Get-ItemProperty $desktopKey -Name 'SCRNSAVE.EXE' -ErrorAction SilentlyContinue).'SCRNSAVE.EXE'
$hadScreenSaver  = $null -ne $origScreenSaver

try {
    Assert-True (Test-Path $SetupExe) "找不到安装包：$SetupExe"

    # --- 基线检查：这些必须都不存在，否则测试不可信 ---
    # 注意不含 $ctxKey：空的右键菜单键正是本安装包要清掉的残留，不能当污染拒绝
    foreach ($p in @($dmtKey, $dataDir, $ssKey, $scrPath, $scrWowPath, $desktopLnk)) {
        Assert-True (-not (Test-Path $p)) "基线已被污染，先手工清理：$p"
    }
    Assert-True (-not (Find-DmtUninstallEntry)) "基线已被污染：程序和功能里还有一个 Dual Monitor Tools 卸载项"
    Assert-True (-not (Test-Path (Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Dual Monitor Tools'))) `
        "基线已被污染：开始菜单里还有 Dual Monitor Tools 组"

    # --- 静默安装 ---
    $exit = Invoke-Program $SetupExe @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir")
    Assert-True ($exit -eq 0) "安装失败，退出码 $exit"
    Assert-True (Test-Path (Join-Path $targetDir 'DMT.exe')) "安装后 DMT.exe 不存在"
    Assert-True (Test-Path (Join-Path $targetDir 'zh-Hans\DMT.resources.dll')) "中文卫星程序集未安装到 zh-Hans 子目录"
    Assert-True (Test-Path $scrPath) "屏保未安装到 System32"
    # 32 位安装包写 {sys} 会被 WOW64 重定向到 SysWOW64，64 位 desk.cpl 就看不到这个屏保
    Assert-True (-not (Test-Path $scrWowPath)) "屏保漏到了 SysWOW64（WOW64 重定向未规避）"
    Assert-True (Test-Path $ssKey) "ScreenSaverInstalled 标记未写入"
    Assert-True (Test-Path $desktopLnk) "桌面快捷方式未创建：$desktopLnk"
    # 原 MSI 留下的空右键菜单键必须被清掉，否则桌面上会多一个点不动的菜单项
    Assert-True (-not (Test-Path $ctxKey)) "空右键菜单键仍然存在（Explorer 会显示成坏菜单项）"
    Write-Host "安装阶段 PASS" -ForegroundColor Green

    # --- 回归：装完之后必须能再装一次 ---
    # ScreenSaverInstalled 与「原版 MSI 已安装」标记在同一个键下；若前置检查
    # 用 RegKeyExists 而不是查 installed 值，这里会被自己的标记挡回去。
    $again = Invoke-Program $SetupExe @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir")
    Assert-True ($again -eq 0) "二次安装被拒绝（退出码 $again）——前置检查把自己写的 ScreenSaverInstalled 当成了原版 MSI"
    Write-Host "二次安装 PASS" -ForegroundColor Green

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

    # 6) 屏保选择指向我们的 .scr（由控制面板写入，不是 DMT 写的，但删文件后会悬空）
    Set-ItemProperty -Path $desktopKey -Name 'SCRNSAVE.EXE' -Value $scrPath

    Write-Host "已制造全部运行时残留" -ForegroundColor Yellow

    # --- 静默卸载 ---
    $unins = Join-Path $targetDir 'unins000.exe'
    Assert-True (Test-Path $unins) "卸载程序不存在"
    $u = Invoke-Program $unins @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
    Assert-True ($u -eq 0) "卸载失败，退出码 $u"

    $gone = Wait-UntilGone (Join-Path $targetDir 'DMT.exe') 90
    Assert-True $gone "卸载 90 秒后 DMT.exe 仍存在，卸载未真正完成"

    # [Code] 的清理在重启后的卸载进程里跑，必须等它做完再断言
    $cleaned = Wait-Until { -not (Test-Path $dmtKey) } 90
    Assert-True $cleaned "卸载 90 秒后 HKCU\SOFTWARE\Dual Monitor Tools 仍存在，[Code] 清理未执行或未完成"

    # --- 断言全部消失 ---
    Assert-True (-not (Test-Path $dmtKey))  "HKCU\SOFTWARE\Dual Monitor Tools 子树未删除"
    Assert-True (-not (Test-Path $ctxKey))  "右键菜单子树未删除（只删空键会留下子命令）"
    $runValue = (Get-ItemProperty $runKey -Name 'GNE_DualMonitorTools' -ErrorAction SilentlyContinue).GNE_DualMonitorTools
    Assert-True ($null -eq $runValue)       "开机自启值未删除"
    Assert-True (-not (Test-Path $dataDir)) "数据目录未删除"
    Assert-True (-not (Test-Path $ssKey))   "ScreenSaverInstalled 标记未删除"
    Assert-True (-not (Test-Path (Split-Path $ssKey -Parent))) "HKLM\...\WOW6432Node\GNE 空键未清理"
    Assert-True (-not (Test-Path $scrPath)) "System32 下的屏保未删除"
    $ss = (Get-ItemProperty $desktopKey -Name 'SCRNSAVE.EXE' -ErrorAction SilentlyContinue).'SCRNSAVE.EXE'
    Assert-True ($null -eq $ss) "SCRNSAVE.EXE 仍指向已删除的屏保（实际值：'$ss'）"

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

    $p2 = Invoke-Program $SetupExe @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir2")
    Assert-True ($p2 -eq 0) "阶段二安装失败，退出码 $p2"

    Set-ItemProperty -Path $desktopKey -Name 'Wallpaper' -Value $foreign

    $u2 = Invoke-Program (Join-Path $targetDir2 'unins000.exe') @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART')
    Assert-True ($u2 -eq 0) "阶段二卸载失败，退出码 $u2"
    Assert-True (Wait-UntilGone (Join-Path $targetDir2 'DMT.exe') 90) "阶段二卸载未真正完成"
    # 同样要等重启后的卸载进程跑完，否则"壁纸被保留"可能只是因为清理还没开始
    Assert-True (Wait-Until { -not (Test-Path $targetDir2) } 90) "阶段二卸载 90 秒后安装目录仍在"

    $w2 = (Get-ItemProperty $desktopKey -Name Wallpaper -ErrorAction SilentlyContinue).Wallpaper
    Assert-True ($w2 -eq $foreign) "卸载误清了用户自己的壁纸（期望 '$foreign'，实际 '$w2'）"

    Write-Host "阶段二 PASS：非 DMT 管理的壁纸被保留" -ForegroundColor Green
}
finally {
    # --- 先从失败路径里脱身：有 unins000.exe 就走正规卸载，避免留下卸载项 ---
    foreach ($dir in @($targetDir, $targetDir2)) {
        if (-not $dir -or -not (Test-Path $dir)) { continue }
        $unins = Join-Path $dir 'unins000.exe'
        if (Test-Path $unins) {
            try {
                Invoke-Program $unins @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') 90 | Out-Null
            } catch {
                Write-Host "兜底卸载失败：$($_.Exception.Message)" -ForegroundColor Yellow
            }
            Wait-UntilGone (Join-Path $dir 'DMT.exe') 60 | Out-Null
        }
    }

    # --- 复原屏保选择 ---
    if ($hadScreenSaver) {
        Set-ItemProperty -Path $desktopKey -Name 'SCRNSAVE.EXE' -Value $origScreenSaver -ErrorAction SilentlyContinue
    } else {
        Remove-ItemProperty -Path $desktopKey -Name 'SCRNSAVE.EXE' -ErrorAction SilentlyContinue
    }

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

    # --- 卸载项与开始菜单组：卸载器自己失败时这两处会留下，基线检查也得管 ---
    foreach ($entry in @(Find-DmtUninstallEntry)) {
        Remove-Item -Path $entry -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $desktopLnk) { Remove-Item -Path $desktopLnk -Force -ErrorAction SilentlyContinue }
    foreach ($group in @((Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Dual Monitor Tools'))) {
        if (Test-Path $group) { Remove-Item -Path $group -Recurse -Force -ErrorAction SilentlyContinue }
    }

    Write-Host "已复原壁纸与清理测试残留" -ForegroundColor Yellow
}
