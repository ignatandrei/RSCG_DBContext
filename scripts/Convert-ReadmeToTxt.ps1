<#
.SYNOPSIS
    Converts the repository's README.md into a plain-text readme.txt.

.DESCRIPTION
    Strips common Markdown syntax (headers, emphasis, links, code fences,
    inline code, blockquotes, list markers, images, HTML tags) so the result
    reads as plain text, while preserving the literal contents of fenced code
    blocks and inline code spans (e.g. `DbSet<T>`) untouched. Intended to keep
    the NuGet package's readme.txt (shown by Visual Studio right after
    install) in sync with README.md (shown in the NuGet UI).

.PARAMETER SourcePath
    Path to the source Markdown file. Defaults to README.md at the repo root.

.PARAMETER DestinationPath
    Path to the plain-text file to write. Defaults to readme.txt at the repo root.

.EXAMPLE
    .\scripts\Convert-ReadmeToTxt.ps1

.EXAMPLE
    .\scripts\Convert-ReadmeToTxt.ps1 -SourcePath .\README.md -DestinationPath .\readme.txt
#>
[CmdletBinding()]
param(
    [string]$SourcePath,
    [string]$DestinationPath
)

$ErrorActionPreference = "Stop"

$scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
if (-not $SourcePath) { $SourcePath = Join-Path $scriptRoot "..\README.md" }
if (-not $DestinationPath) { $DestinationPath = Join-Path $scriptRoot "..\readme.txt" }

$SourcePath = (Resolve-Path -LiteralPath $SourcePath).Path

if (-not (Test-Path -LiteralPath $SourcePath)) {
    throw "Source markdown file not found: $SourcePath"
}

$text = Get-Content -LiteralPath $SourcePath -Raw

# --- Step 1: protect fenced code blocks and inline code spans from later transforms ---
$blocks = New-Object System.Collections.Generic.List[string]
$inlineCode = New-Object System.Collections.Generic.List[string]

$text = [regex]::Replace(
    $text,
    '```[a-zA-Z0-9_-]*\r?\n(.*?)```',
    {
        param($m)
        $blocks.Add($m.Groups[1].Value.TrimEnd("`r", "`n"))
        "`n@@CODEBLOCK$($blocks.Count - 1)@@`n"
    },
    [System.Text.RegularExpressions.RegexOptions]::Singleline)

$text = [regex]::Replace(
    $text,
    '`([^`\r\n]+)`',
    {
        param($m)
        $inlineCode.Add($m.Groups[1].Value)
        "@@INLINECODE$($inlineCode.Count - 1)@@"
    })

# --- Step 2: strip remaining Markdown syntax from prose ---

# Images: ![alt](url) -> alt
$text = [regex]::Replace($text, '!\[([^\]]*)\]\(([^)]*)\)', '$1')

# Links: [text](url) -> text (url)
$text = [regex]::Replace($text, '\[([^\]]+)\]\(([^)]+)\)', '$1 ($2)')

# Bold/italic/strikethrough markers (asterisk-based; underscores are left alone
# so identifiers like Exists_AllDBSets are not affected)
$text = [regex]::Replace($text, '\*\*\*(.+?)\*\*\*', '$1')
$text = [regex]::Replace($text, '\*\*(.+?)\*\*', '$1')
$text = [regex]::Replace($text, '(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)', '$1')
$text = [regex]::Replace($text, '~~(.+?)~~', '$1')

# Headings: "# Title" -> "Title"
$text = [regex]::Replace($text, '(?m)^\s{0,3}#{1,6}\s+', '')

# Blockquotes: "> text" -> "text"
$text = [regex]::Replace($text, '(?m)^\s{0,3}>\s?', '')

# Unordered list markers: "- item" / "* item" / "+ item" -> "- item"
$text = [regex]::Replace($text, '(?m)^(\s*)[-*+]\s+', '$1- ')

# Horizontal rules: "---" / "***" / "___" on their own line -> removed
$text = [regex]::Replace($text, '(?m)^\s{0,3}(-{3,}|\*{3,}|_{3,})\s*$', '')

# Raw HTML tags in prose
$text = [regex]::Replace($text, '<[^>]+>', '')

# --- Step 3: restore protected content, unmodified ---
$text = [regex]::Replace($text, '@@INLINECODE(\d+)@@', { param($m) $inlineCode[[int]$m.Groups[1].Value] })
$text = [regex]::Replace($text, '@@CODEBLOCK(\d+)@@', { param($m) $blocks[[int]$m.Groups[1].Value] })

# Collapse 3+ blank lines to a single blank line
$text = [regex]::Replace($text, '(\r?\n){3,}', "`n`n")

$text = $text.Trim() + "`r`n"

Set-Content -LiteralPath $DestinationPath -Value $text -NoNewline -Encoding utf8

Write-Host "Wrote $DestinationPath from $SourcePath"
