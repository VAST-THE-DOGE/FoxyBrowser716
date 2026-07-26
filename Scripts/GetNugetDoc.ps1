param(
    [Parameter(Mandatory)] [string]$Package,
    [string]$Type,
    [string]$Member,
    [string]$Project = 'FoxyBrowser716',
    [switch]$Raw
)
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$assetsPath = if (Test-Path $Project) { Join-Path $Project 'obj\project.assets.json' }
              else { Join-Path $repoRoot "$Project\obj\project.assets.json" }
if (-not (Test-Path $assetsPath)) { throw "No assets file at $assetsPath - restore the project first" }

$assets = Get-Content $assetsPath -Raw | ConvertFrom-Json
$libKey = $assets.libraries.PSObject.Properties.Name | Where-Object { $_ -like "$Package/*" } | Select-Object -First 1
if (-not $libKey) {
    $near = $assets.libraries.PSObject.Properties.Name | Where-Object { $_ -match [regex]::Escape($Package) }
    if ($near) { throw "Package '$Package' not found. Similar: $($near -join ', ')" }
    throw "Package '$Package' not found in $assetsPath"
}

$pkgDir = $null
foreach ($root in $assets.packageFolders.PSObject.Properties.Name) {
    $candidate = Join-Path $root $libKey.ToLowerInvariant()
    if (Test-Path $candidate) { $pkgDir = $candidate; break }
}
if (-not $pkgDir) { throw "Package folder for $libKey not found in any packageFolders root" }

$xml = $null
$tfmTarget = $assets.targets.PSObject.Properties | Select-Object -First 1
$compile = $tfmTarget.Value.$libKey.compile
if ($compile) {
    $dllRel = $compile.PSObject.Properties.Name | Where-Object { $_ -like '*.dll' } | Select-Object -First 1
    if ($dllRel) {
        $candidate = Join-Path $pkgDir ($dllRel -replace '\.dll$', '.xml').Replace('/', '\')
        if (Test-Path $candidate) { $xml = Get-Item $candidate }
    }
}
if (-not $xml) {
    $xml = Get-ChildItem $pkgDir -Recurse -Filter '*.xml' |
        Where-Object { $_.FullName -match '\\(lib|ref)\\' } |
        Sort-Object { $_.FullName -notmatch '\\lib\\' }, FullName |
        Select-Object -First 1
}
if (-not $xml) {
    Write-Host "No xml docs in $libKey - Scripts/GetNugetApi.ps1 reflects the public surface instead. Assemblies:"
    Get-ChildItem $pkgDir -Recurse -Filter '*.dll' | ForEach-Object { $_.FullName.Substring($pkgDir.Length + 1) }
    exit 1
}

function Get-TypeNames {
    $r = [System.Xml.XmlReader]::Create($xml.FullName)
    try {
        while ($r.ReadToFollowing('member')) {
            $n = $r.GetAttribute('name')
            if ($n -and $n.StartsWith('T:')) { $n.Substring(2) }
        }
    } finally { $r.Dispose() }
}

function Show-Types([string[]]$names) {
    $names | Group-Object { if ($_ -match '^(.*)\.[^.]+$') { $Matches[1] } else { '(global)' } } |
        Sort-Object Name | ForEach-Object {
            "## $($_.Name)"
            (($_.Group | ForEach-Object { ($_ -split '\.')[-1] } | Sort-Object) -join ', ')
            ''
        }
}

function Format-DocText([string]$s) {
    $s = $s -replace '\s+', ' '
    $shortCref = { param($m) ((($m.Groups[1].Value -split '\(')[0]) -split '[.+]')[-1] }
    $s = [regex]::Replace($s, '<see\s+cref="[A-Z]:([^"]+)"\s*/>', $shortCref)
    $s = [regex]::Replace($s, '<see\s+cref="[A-Z]:([^"]+)"[^>]*>.*?</see>', $shortCref)
    $throwsCref = { param($m) "`nThrows " + ((($m.Groups[1].Value -split '\(')[0]) -split '[.+]')[-1] + ': ' }
    $s = [regex]::Replace($s, '<exception\s+cref="[A-Z]:([^"]+)"\s*>', $throwsCref)
    $s = $s -replace '<see\s+href="[^"]*"\s*>(.*?)</see>', '$1'
    $s = $s -replace '<see\s+langword="([^"]*)"\s*/>', '$1'
    $s = $s -replace '<(?:param|typeparam)ref\s+name="([^"]*)"\s*/>', '$1'
    $s = $s -replace '<(?:param|typeparam)\s+name="([^"]*)"\s*>', "`n- `$1: "
    $s = $s -replace '<returns\s*>', "`nReturns: "
    $s = $s -replace '<value\s*>', "`nValue: "
    $s = $s -replace '<inheritdoc[^>]*/>', '(inheritdoc)'
    $s = $s -replace '</?(?:summary|remarks|para|c|code|b|i|em|param|typeparam|returns|value|exception|list|item|term|description)\s*>', ' '
    $s = $s -replace '<[^>]+>', ''
    (($s -split "`n" | ForEach-Object { ($_ -replace '\s+', ' ').Trim() } | Where-Object { $_ }) -join "`n")
}

function Format-MemberName([string]$name, [string]$typeFull) {
    $kind = $name[0]
    $n = $name.Substring(2)
    if ($n -eq $typeFull) { return "$kind $((($typeFull -split '[.+]')[-1]))" }
    if ($n.StartsWith("$typeFull.")) { $n = $n.Substring($typeFull.Length + 1) }
    $n = [regex]::Replace($n, '(?:\w+\.)+(?=[\w#])', '')
    "$kind $n"
}

$script:docCount = 0
function Write-Docs([string]$typeFull) {
    $pattern = '^[TMPFE]:' + [regex]::Escape($typeFull) + '($|[.(])'
    $r = [System.Xml.XmlReader]::Create($xml.FullName)
    try {
        if (-not $r.ReadToFollowing('member')) { return }
        while ($true) {
            $name = $r.GetAttribute('name')
            $hit = $name -and $name -match $pattern -and
                   (-not $Member -or $name.StartsWith('T:') -or $name -match [regex]::Escape($Member))
            if ($hit) {
                $script:docCount++
                $body = $r.ReadInnerXml()
                "## $(Format-MemberName $name $typeFull)"
                if ($Raw) { ($body -replace '\s+', ' ').Trim() } else { Format-DocText $body }
                ''
            } else {
                $r.Skip()
            }
            if ($r.NodeType -eq [System.Xml.XmlNodeType]::Element -and $r.Name -eq 'member') { continue }
            if (-not $r.ReadToFollowing('member')) { break }
        }
    } finally { $r.Dispose() }
}

Write-Host "# $libKey -> $($xml.FullName.Substring($pkgDir.Length + 1))`n"

$typeNames = @(Get-TypeNames)

if (-not $Type) {
    Show-Types $typeNames
    exit 0
}

$target = $null
if ($typeNames -contains $Type) {
    $target = $Type
} elseif ($Type -notmatch '\.') {
    $candidates = @($typeNames | Where-Object { ((($_ -split '[.+]')[-1]) -replace '`.*$', '') -eq $Type })
    if ($candidates.Count -eq 1) { $target = $candidates[0] }
    elseif ($candidates.Count -gt 1) {
        Write-Host "Multiple types named '$Type' - rerun with the full name:"
        $candidates | ForEach-Object { "  $_" }
        exit 1
    }
}
if (-not $target) {
    $inNs = @($typeNames | Where-Object { $_.StartsWith("$Type.") })
    if ($inNs.Count -gt 0) {
        Write-Host "Types in namespace '$Type':"
        Show-Types $inNs
        exit 0
    }
    $similar = @($typeNames | Where-Object { $_ -match [regex]::Escape($Type) })
    if ($similar.Count -gt 0 -and $similar.Count -le 40) {
        Write-Host "No exact match for '$Type'. Similar:"
        $similar | ForEach-Object { "  $_" }
    } else {
        Write-Host "Type '$Type' not found. Run without -Type to list all types."
    }
    exit 1
}

Write-Docs $target
if ($script:docCount -eq 0) { Write-Host "No members of $target matched '$Member'." }
