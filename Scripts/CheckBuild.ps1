param(
    [string]$Project = 'FoxyBrowser716/FoxyBrowser716.csproj',
    [string]$Platform = 'x64'
)
$repoRoot = Split-Path $PSScriptRoot -Parent
$target = if (Test-Path $Project) { $Project } else { Join-Path $repoRoot $Project }
if (-not (Test-Path $target)) { throw "No such project/sln: $Project" }

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$out = dotnet build $target -p:Platform=$Platform -v:q -nologo 2>&1 | ForEach-Object { "$_" }
$code = $LASTEXITCODE
$sw.Stop()

$diagnostics = $out | Where-Object { $_ -match '(?i)\b(error|warning)\s+[A-Za-z]+\d+' } | Sort-Object -Unique
$diagnostics
if ($code -ne 0 -and -not $diagnostics) { $out | Select-Object -Last 15 }

$elapsed = [Math]::Round($sw.Elapsed.TotalSeconds, 1)
if ($code -eq 0) { Write-Host "OK (${elapsed}s) $Project [$Platform]" } else { Write-Host "FAILED exit $code (${elapsed}s) $Project [$Platform]" }
exit $code
