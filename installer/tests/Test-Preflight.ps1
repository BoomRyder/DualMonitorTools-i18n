<#
    验证前置检查：伪造"原版 MSI 已安装"标记后，安装包必须拒绝安装。
    需要管理员权限。测试后完整复原注册表。
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
    $p = Start-Process -FilePath $SetupExe `
        -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', "/DIR=$targetDir" `
        -Wait -PassThru

    Write-Host "安装程序退出码：$($p.ExitCode)"
    Assert-True ($p.ExitCode -ne 0) "存在旧 MSI 时安装程序必须拒绝安装（退出码应非 0）"
    Assert-True (-not (Test-Path (Join-Path $targetDir 'DMT.exe'))) "被拒绝时不应安装任何文件"

    Write-Host "PASS：前置检查正确拦截了旧 MSI 共存" -ForegroundColor Green
}
finally {
    # --- 复原 ---
    if (Test-Path $legacyKey) { Remove-Item -Path $legacyKey -Recurse -Force }
    if (Test-Path $legacyParent) {
        $children = Get-ChildItem $legacyParent -ErrorAction SilentlyContinue
        if (-not $children) { Remove-Item -Path $legacyParent -Force -ErrorAction SilentlyContinue }
    }
    if (Test-Path $targetDir) { Remove-Item -Path $targetDir -Recurse -Force }
    Write-Host "已复原注册表与临时目录" -ForegroundColor Yellow
}
