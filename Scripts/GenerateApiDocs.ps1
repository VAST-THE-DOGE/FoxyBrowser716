$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$configPath = Join-Path $repoRoot 'docfx.json'
$outDir = Join-Path $repoRoot 'Docs/api'

function Clean-ApiMarkdown([string]$path) {
    $lines = [System.IO.File]::ReadAllLines($path)
    $kept = [System.Collections.Generic.List[string]]::new()
    $skippingInherited = $false

    foreach ($line in $lines) {
        if ($skippingInherited) {
            if ($line -match '^#') { $skippingInherited = $false } else { continue }
        }
        if ($line -match '^#{1,6}\s+Inherited Members\s*$') {
            $skippingInherited = $true
            continue
        }

        $cleaned = $line -replace '<a\s+id="[^"]*"\s*>\s*</a>\s*', ''
        if ($cleaned -eq '' -and $line -ne '' -and $kept.Count -gt 0 -and $kept[$kept.Count - 1] -eq '') {
            continue
        }
        $kept.Add($cleaned)
    }

    [System.IO.File]::WriteAllLines($path, $kept)
}

if (-not (Get-Command docfx -ErrorAction SilentlyContinue)) {
    Write-Host 'docfx not found, installing as global dotnet tool...'
    dotnet tool install -g docfx
    if ($LASTEXITCODE -ne 0) { throw 'docfx install failed' }
}

if (Test-Path $outDir) {
    Remove-Item $outDir -Recurse -Force
}

docfx metadata $configPath
if ($LASTEXITCODE -ne 0) { throw "docfx metadata failed with exit code $LASTEXITCODE" }

$mdFiles = Get-ChildItem $outDir -Filter '*.md' -Recurse
foreach ($file in $mdFiles) { Clean-ApiMarkdown $file.FullName }

# a handful of files can still be mid-flush from docfx on the first pass
for ($pass = 0; $pass -lt 3; $pass++) {
    $dirty = @(Get-ChildItem $outDir -Filter '*.md' -Recurse |
        Select-String -Pattern '<a id=|^#{1,6}\s+Inherited Members\s*$' -List)
    if ($dirty.Count -eq 0) { break }
    Start-Sleep -Milliseconds 500
    foreach ($hit in $dirty) { Clean-ApiMarkdown $hit.Path }
}
if ($dirty.Count -gt 0) {
    Write-Warning "$($dirty.Count) files still contain anchors/inherited-member blocks after retries"
}

$commit = git -C $repoRoot rev-parse --short HEAD
Set-Content (Join-Path $outDir '_generated.txt') "generated: $([DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm'))Z`ncommit: $commit"

Write-Host "Generated and cleaned $($mdFiles.Count) markdown files in $outDir"
