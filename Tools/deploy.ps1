#requires -Version 7
<#
.SYNOPSIS
    PersonalFoodPreferences 建置＋部署（net472 單一 DLL → 1.6／1.7；XML 同步到遊戲 Mods）。

.DESCRIPTION
    1. 遊戲執行中就中止（DLL 被鎖）
    2. dotnet build net472：OutputPath 寫 1.6\Assemblies，SyncAssembliesTo17 複製到 1.7\Assemblies
    3. 核對兩份 DLL hash 一致
    4. 複製 DLL 到 D 側 Mods 的 1.6／1.7
    5. Common／About／LoadFolders 有差異時同步並核對 hash（-Check 只比對）

.EXAMPLE
    pwsh -File Tools/deploy.ps1 -Check   # 只比對，不建置、不寫入
    pwsh -File Tools/deploy.ps1          # build＋部署
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Dest = 'D:\Game\Steam\steamapps\common\RimWorld\Mods\PersonalFoodPreferences',
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
$project = Join-Path $root 'Source\PersonalFoodPreferences.csproj'
$dll16 = Join-Path $root '1.6\Assemblies\PersonalFoodPreferences.dll'
$dll17 = Join-Path $root '1.7\Assemblies\PersonalFoodPreferences.dll'

if (-not (Test-Path -LiteralPath $Dest)) { throw "部署目標不存在：$Dest" }

if (-not $Check) {
    $running = Get-Process -Name 'RimWorldWin64', 'RimWorld' -ErrorAction SilentlyContinue
    if ($running) { throw "RimWorld 執行中（pid $($running.Id -join ',')）：先關遊戲再 build／部署。" }
    if (-not (Test-Path -LiteralPath $dotnet)) { throw "找不到使用者層級 dotnet：$dotnet" }

    Write-Host "[1/4] build net472 $Configuration ..."
    & $dotnet build $project -c $Configuration --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { throw "build 失敗（exit $LASTEXITCODE）" }
}

foreach ($p in $dll16, $dll17) { if (-not (Test-Path -LiteralPath $p)) { throw "找不到產物：$p" } }
$h16 = (Get-FileHash -LiteralPath $dll16 -Algorithm MD5).Hash
$h17 = (Get-FileHash -LiteralPath $dll17 -Algorithm MD5).Hash
if ($h16 -ne $h17) { throw "1.6 與 1.7 DLL 不一致：$h16 / $h17" }
$info = Get-Item -LiteralPath $dll16
Write-Host ("[2/4] DLL {0:N1} KB  {1:yyyy-MM-dd HH:mm:ss}  MD5={2}" -f ($info.Length / 1KB), $info.LastWriteTime, $h16)

# XML／About／LoadFolders 差異
$items = @()
foreach ($sub in 'Common', 'About') { $items += Get-ChildItem -LiteralPath (Join-Path $root $sub) -Recurse -File }
$items += Get-Item -LiteralPath (Join-Path $root 'LoadFolders.xml')
$diff = @()
foreach ($item in $items) {
    $rel = [System.IO.Path]::GetRelativePath($root, $item.FullName)
    $target = Join-Path $Dest $rel
    if (-not (Test-Path -LiteralPath $target)) { $diff += "+ $rel"; continue }
    if ((Get-FileHash -LiteralPath $item.FullName -Algorithm MD5).Hash -ne (Get-FileHash -LiteralPath $target -Algorithm MD5).Hash) { $diff += "~ $rel" }
}
Write-Host ("[3/4] XML／About 差異：{0} 檔" -f $diff.Count)
$diff | Select-Object -First 15 | ForEach-Object { Write-Host "      $_" -ForegroundColor Yellow }

$dllDiff = @()
foreach ($ver in '1.6', '1.7') {
    $src = Join-Path $root "$ver\Assemblies\PersonalFoodPreferences.dll"
    $dst = Join-Path $Dest "$ver\Assemblies\PersonalFoodPreferences.dll"
    if (-not (Test-Path -LiteralPath $dst)) { $dllDiff += "+ $ver DLL" }
    elseif ((Get-FileHash -LiteralPath $src -Algorithm MD5).Hash -ne (Get-FileHash -LiteralPath $dst -Algorithm MD5).Hash) { $dllDiff += "~ $ver DLL" }
}
Write-Host ("      DLL：{0}" -f $(if ($dllDiff.Count -eq 0) { '兩份皆相同' } else { $dllDiff -join '、' }))
if ($dllDiff) { $dllDiff | ForEach-Object { Write-Host "      $_" -ForegroundColor Yellow } }

if ($Check) {
    Write-Host '[4/4] -Check：只比對，未建置、未寫入。' -ForegroundColor Cyan
    exit 0
}

Write-Host '[4/4] 同步 ...'
foreach ($ver in '1.6', '1.7') {
    $dst = Join-Path $Dest "$ver\Assemblies\PersonalFoodPreferences.dll"
    New-Item -ItemType Directory -Path (Split-Path -Parent $dst) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root "$ver\Assemblies\PersonalFoodPreferences.dll") -Destination $dst -Force
}
foreach ($line in $diff) {
    $rel = $line.Substring(2)
    $target = Join-Path $Dest $rel
    New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $root $rel) -Destination $target -Force
}
foreach ($ver in '1.6', '1.7') {
    $src = Join-Path $root "$ver\Assemblies\PersonalFoodPreferences.dll"
    $dst = Join-Path $Dest "$ver\Assemblies\PersonalFoodPreferences.dll"
    if ((Get-FileHash -LiteralPath $src -Algorithm MD5).Hash -ne (Get-FileHash -LiteralPath $dst -Algorithm MD5).Hash) { throw "$ver DLL 同步後 hash 不一致" }
}
Write-Host ("      OK  已同步 {0} 個 XML／About 檔＋2 份 DLL，hash 全部一致；重啟遊戲生效。" -f $diff.Count) -ForegroundColor Green
