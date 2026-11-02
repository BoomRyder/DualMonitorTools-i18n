#Requires -RunAsAdministrator
<#
    验证前置检查：伪造"原版 MSI 已安装"标记后，安装包必须拒绝安装。
    需要管理员权限。测试后完整复原注册表；若前置检查失效导致真的装上，也会卸载干净。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SetupExe
)

$ErrorActionPreference = 'Stop'

$legacyKey = 'HKLM:\SOFTWARE\WOW6432Node\GNE\Dual Monitor Tools'
$legacyParent = 'HKLM:\SOFTWARE\WOW6432Node\GNE'
$targetDir = Join-Path $env:TEMP ('dmt_preflight_' + [guid]::NewGuid().ToString('N'))

function Assert-True($cond, $msg) {
    if (-not $cond) { throw "断言失败：$msg" }
}

function Invoke-Program {
    # 用 CreateProcess（UseShellExecute=$false）而不是 Start-Process：后者走
    # ShellExecute，会因 MOTW / 杀软检查弹出"打开文件 - 安全警告"对话框。
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

try {
    if (-not (Test-Path $SetupExe)) { throw "找不到安装包：$SetupExe" }

    # --- 前提：基线应不存在 ---
    if (Test-Path $legacyKey) {
        throw "基线已被污染：$legacyKey 已存在。请先清理后再跑。"
    }

    # --- 伪造原版 MSI 的 installed 标记 ---
    New-Item -Path $legacyKey -Force | Out-Null
    New-ItemProperty -Path $legacyKey -Name 'installed' -Value 1 -PropertyType DWord -Force | Out-Null
    Write-Host "已伪造原版 MSI 标记：$legacyKey" -ForegroundColor Yellow

    # --- 静默安装，应当被拒绝 ---
    $p = Invoke-Program $SetupExe @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir")

    Write-Host "安装程序退出码：$p"
    Assert-True ($p -ne 0) "存在旧 MSI 时安装程序必须拒绝安装（退出码应非 0）"
    Assert-True (-not (Test-Path (Join-Path $targetDir 'DMT.exe'))) "被拒绝时不应安装任何文件"

    Write-Host "PASS：前置检查正确拦截了旧 MSI 共存" -ForegroundColor Green
}
finally {
    # --- 先脱身：前置检查一旦失效，安装会真的落地，必须走正规卸载 ---
    $unins = Join-Path $targetDir 'unins000.exe'
    if (Test-Path $unins) {
        try {
            Invoke-Program $unins @('/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART') 90 | Out-Null
        } catch {
            Write-Host "兜底卸载失败：$($_.Exception.Message)" -ForegroundColor Yellow
        }
        Start-Sleep -Milliseconds 1500
    }

    # --- 复原注册表 ---
    if (Test-Path $legacyKey) { Remove-Item -Path $legacyKey -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $legacyParent) {
        $children = Get-ChildItem $legacyParent -ErrorAction SilentlyContinue
        if (-not $children) { Remove-Item -Path $legacyParent -Force -ErrorAction SilentlyContinue }
    }

    # --- 清理可能落地的安装残留 ---
    foreach ($f in @((Join-Path $env:WINDIR 'System32\DmtWallpaper.scr'),
                     (Join-Path $env:WINDIR 'SysWOW64\DmtWallpaper.scr'))) {
        if (Test-Path $f) { Remove-Item -Path $f -Force -ErrorAction SilentlyContinue }
    }
    foreach ($root in @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
                        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall')) {
        if (-not (Test-Path $root)) { continue }
        Get-ChildItem $root -ErrorAction SilentlyContinue | ForEach-Object {
            $dn = (Get-ItemProperty $_.PSPath -Name DisplayName -ErrorAction SilentlyContinue).DisplayName
            if ($dn -eq 'Dual Monitor Tools') {
                Remove-Item -Path $_.PSPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
    $group = Join-Path $env:ProgramData 'Microsoft\Windows\Start Menu\Programs\Dual Monitor Tools'
    if (Test-Path $group) { Remove-Item -Path $group -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path $targetDir) { Remove-Item -Path $targetDir -Recurse -Force -ErrorAction SilentlyContinue }

    Write-Host "已复原注册表与清理测试残留" -ForegroundColor Yellow
}
