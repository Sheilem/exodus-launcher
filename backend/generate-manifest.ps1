<#
  generate-manifest.ps1 — Exodus Launcher patch-manifest generator
  --------------------------------------------------------------
  Scans a built game folder and emits manifest.json: every file with its
  size + SHA-256. The launcher compares this against the player's install
  and downloads only what changed.

  Usage:
    ./generate-manifest.ps1 -GameDir "D:\Gal\Exodus" -OutDir ".\dist" -BaseUrl "https://cdn.exodus.example/files" -Version "1.0.5"

  Then upload everything in -OutDir (manifest.json + the mirrored files under \files)
  to any static host / object storage (OVH S3, Cloudflare R2, Vercel, plain nginx).
#>
param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [string]$OutDir = ".\dist",
    [string]$BaseUrl = "http://localhost:8777/files",
    [string]$Version = "1.0.0",
    # Files/folders that are per-player or launcher-owned: never patch these.
    [string[]]$Exclude = @("patchcache.json", "launcher.config.json", "ExodusLauncher.exe", "logs", "*.log", "*.part")
)

$ErrorActionPreference = "Stop"
$GameDir = (Resolve-Path $GameDir).Path
$filesOut = Join-Path $OutDir "files"
New-Item -ItemType Directory -Force -Path $filesOut | Out-Null

function Is-Excluded($rel) {
    foreach ($pat in $Exclude) { if ($rel -like $pat -or $rel -like "$pat/*") { return $true } }
    return $false
}

$entries = New-Object System.Collections.Generic.List[object]
$all = Get-ChildItem -Path $GameDir -Recurse -File
$i = 0
foreach ($f in $all) {
    $i++
    $rel = $f.FullName.Substring($GameDir.Length).TrimStart('\', '/').Replace('\', '/')
    if (Is-Excluded $rel) { continue }
    Write-Progress -Activity "Hashing" -Status $rel -PercentComplete (($i / $all.Count) * 100)

    $hash = (Get-FileHash -Algorithm SHA256 -Path $f.FullName).Hash.ToLower()
    $entries.Add([ordered]@{ path = $rel; size = $f.Length; sha256 = $hash })

    # Mirror the file into dist/files so it can be served as-is
    $dest = Join-Path $filesOut $rel
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Copy-Item $f.FullName $dest -Force
}

$manifest = [ordered]@{
    version       = $Version
    published_utc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    base_url      = $BaseUrl
    client_exe    = "Exodus.exe"
    files         = $entries
}

$json = $manifest | ConvertTo-Json -Depth 6
$manifestPath = Join-Path $OutDir "manifest.json"
[System.IO.File]::WriteAllText($manifestPath, $json, (New-Object System.Text.UTF8Encoding($false)))

Write-Host ""
Write-Host "Wrote $($entries.Count) entries -> $manifestPath"
Write-Host "Mirrored files -> $filesOut"
Write-Host "Upload the contents of '$OutDir' to your host; point launcher.config.json:manifest_url at manifest.json."
