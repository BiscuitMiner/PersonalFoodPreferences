#requires -Version 7
<#
.SYNOPSIS
    PersonalFoodPreferences 文檔／XML 一致性檢查（Phase 收尾門）。

.DESCRIPTION
    只檢查、只回報，不改任何檔案。項目：
      1. XML well-formed（Common、About、LoadFolders）
      2. 頂層 Def 的 defName 唯一（不含 FoodOverrideMapDefs 內部的 nested defName 資料）
      3. 三語 Keyed 對齊（英／簡／繁）＋佔位符一致
      4. DefInjected 的 defName 存在，或為 patch 目標的外部 Def
      5. Docs／AGENTS 提到的 Docs/、Source/、Common/、About/ 路徑存在
    有問題 exit 1。

.EXAMPLE
    pwsh -File Tools/check-docs.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$issues = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-Issue([string]$group, [string]$message) { $script:issues.Add("[$group] $message") }
function Join-Root([string]$rel) { Join-Path $root $rel }

# ---------- 1. XML well-formed ----------
$xmlRoots = @('Common', 'About\About.xml', 'LoadFolders.xml')
$xmlDocs = [System.Collections.Generic.List[object]]::new()
foreach ($rel in $xmlRoots) {
    $target = Join-Root $rel
    if (-not (Test-Path -LiteralPath $target)) { Add-Issue 'xml' "缺少路徑：$rel"; continue }
    $files = if ((Get-Item -LiteralPath $target) -is [System.IO.DirectoryInfo]) {
        Get-ChildItem -LiteralPath $target -Recurse -File -Filter *.xml
    } else { Get-Item -LiteralPath $target }
    foreach ($file in $files) {
        try {
            $doc = [xml](Get-Content -LiteralPath $file.FullName -Raw)
            $xmlDocs.Add([pscustomobject]@{ Path = $file.FullName; Doc = $doc; Rel = [System.IO.Path]::GetRelativePath($root, $file.FullName) })
        } catch {
            Add-Issue 'xml' "$([System.IO.Path]::GetRelativePath($root, $file.FullName)) 解析失敗：$($_.Exception.Message)"
        }
    }
}

# ---------- 2. 頂層 defName 唯一 ----------
$defNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$defsRoot = Join-Root 'Common\Defs'
foreach ($entry in $xmlDocs) {
    if (-not $entry.Path.StartsWith($defsRoot, [System.StringComparison]::OrdinalIgnoreCase)) { continue }
    # 只取 /Defs/*/defName（override map 內的 <overrides><li><defName> 是資料，不是 Def 定義）
    foreach ($node in $entry.Doc.SelectNodes('/Defs/*/defName')) {
        $name = $node.InnerText.Trim()
        if ([string]::IsNullOrWhiteSpace($name)) { Add-Issue 'def' "$($entry.Rel) 有空 defName"; continue }
        if (-not $defNames.Add($name)) { Add-Issue 'def' "defName 重複：$name（$($entry.Rel)）" }
    }
}

# ---------- 3. 三語 Keyed 對齊 ----------
$langRoot = Join-Root 'Common\Languages'
function Get-Keyed([string]$lang) {
    $result = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
    $dir = Join-Path $langRoot "$lang\Keyed"
    if (-not (Test-Path -LiteralPath $dir)) { Add-Issue 'keyed' "缺少 Keyed 目錄：$lang"; return $result }
    foreach ($file in Get-ChildItem -LiteralPath $dir -Recurse -File -Filter *.xml) {
        try { $doc = [xml](Get-Content -LiteralPath $file.FullName -Raw) } catch { continue }
        foreach ($node in $doc.DocumentElement.ChildNodes) {
            if ($node.NodeType -ne 'Element') { continue }
            if ($result.ContainsKey($node.LocalName)) { Add-Issue 'keyed' "$lang key 重複：$($node.LocalName)（$($file.Name)）" }
            else { $result[$node.LocalName] = $node.InnerText }
        }
    }
    return $result
}

$en = Get-Keyed 'English'
foreach ($lang in 'ChineseSimplified', 'ChineseTraditional') {
    $other = Get-Keyed $lang
    foreach ($key in $en.Keys) { if (-not $other.ContainsKey($key)) { Add-Issue 'keyed' "$lang 缺 key：$key" } }
    foreach ($key in $other.Keys) { if (-not $en.ContainsKey($key)) { Add-Issue 'keyed' "英文缺 key：$key（$lang 有）" } }
    foreach ($key in $en.Keys) {
        if (-not $other.ContainsKey($key)) { continue }
        $a = ([regex]::Matches($en[$key], '\{\d+\}') | ForEach-Object Value | Sort-Object) -join ','
        $b = ([regex]::Matches($other[$key], '\{\d+\}') | ForEach-Object Value | Sort-Object) -join ','
        if ($a -ne $b) { Add-Issue 'keyed' "佔位符不一致：$key（en=$a / $lang=$b）" }
    }
}

# ---------- 4. DefInjected defName 存在 ----------
$patchText = (@(Get-ChildItem -LiteralPath (Join-Root 'Common\Patches') -File -Filter *.xml | ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }) -join "`n")
foreach ($file in Get-ChildItem -LiteralPath $langRoot -Recurse -File -Filter *.xml | Where-Object FullName -match 'DefInjected') {
    try { $doc = [xml](Get-Content -LiteralPath $file.FullName -Raw) } catch { continue }
    foreach ($node in $doc.DocumentElement.ChildNodes) {
        if ($node.NodeType -ne 'Element') { continue }
        $defName = $node.LocalName.Split('.')[0]
        if (-not $defNames.Contains($defName) -and -not ($patchText -match [regex]::Escape($defName))) {
            Add-Issue 'DefInjected' "$($node.LocalName) 的 defName 不存在，也不是 patch 目標：$defName（$([System.IO.Path]::GetRelativePath($root, $file.FullName))）"
        }
    }
}

# ---------- 5. 文檔路徑存在 ----------
$mdFiles = @(Get-ChildItem -LiteralPath (Join-Root 'Docs') -Recurse -File -Filter *.md)
$mdFiles += Get-Item (Join-Root 'README.md'), (Join-Root 'README_zh.md'), (Join-Root 'AGENTS.md')
$pathCount = 0
foreach ($file in $mdFiles) {
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $text = [regex]::Replace($text, '(?ms)^(```|~~~).*?^\1', '')   # 忽略 code fence 內範例
    $matches = @()
    $matches += [regex]::Matches($text, '\[[^\]]*\]\(([^)]+)\)') | ForEach-Object { $_.Groups[1].Value }
    $matches += [regex]::Matches($text, '`((?:Docs|Source|Common|About)/[A-Za-z0-9_./%*\-\u4e00-\u9fff]+)') | ForEach-Object { $_.Groups[1].Value }
    foreach ($target in $matches) {
        if ($target -match '^(https?:|mailto:|#)') { continue }
        $clean = $target.Split('#')[0]
        $clean = [System.Uri]::UnescapeDataString($clean)
        if ([string]::IsNullOrWhiteSpace($clean) -or $clean -match '\*') { continue }
        $pathCount++
        # 文檔有兩種寫法：相對本檔（連結）與相對專案根（反引號路徑），兩者都試
        $fromFile = [System.IO.Path]::GetFullPath((Join-Path $file.DirectoryName $clean))
        $fromRoot = [System.IO.Path]::GetFullPath((Join-Path $root $clean))
        if (-not (Test-Path -LiteralPath $fromFile) -and -not (Test-Path -LiteralPath $fromRoot)) {
            Add-Issue 'path' "$([System.IO.Path]::GetRelativePath($root, $file.FullName)) → $target"
        }
    }
}

# ---------- 輸出 ----------
Write-Host "PersonalFoodPreferences 文檔／XML 檢查（$root）"
Write-Host ("  XML：{0} 檔｜頂層 defName：{1}｜Keyed en={2}｜文檔路徑：{3}" -f $xmlDocs.Count, $defNames.Count, $en.Count, $pathCount)
foreach ($warning in $warnings) { Write-Host "  warn  $warning" -ForegroundColor Yellow }
if ($issues.Count -eq 0) {
    Write-Host '  OK  無不一致' -ForegroundColor Green
    exit 0
}
foreach ($issue in $issues) { Write-Host "  FAIL  $issue" -ForegroundColor Red }
Write-Host ("  共 {0} 項，修正後重跑。" -f $issues.Count) -ForegroundColor Red
exit 1
