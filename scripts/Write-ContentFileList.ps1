param(
    [string]$Zip = "$PSScriptRoot\..\artifacts\ULTRAKILL-Content.zip",
    [string]$Output = "$PSScriptRoot\..\artifacts\filelist.txt"
)

# Builds filelist.txt for an existing content zip, so the phone copy can be checked without rebuilding the zip.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $Zip))
try {
    $lines = $archive.Entries |
        ForEach-Object { $name = $_.FullName.Replace('\', '/'); if ($name.StartsWith('./')) { $name = $name.Substring(2) }; [pscustomobject]@{ Name = $name; Length = $_.Length } } |
        Where-Object { $_.Name.StartsWith('ULTRAKILL_Data/') -and -not $_.Name.EndsWith('/') } |
        ForEach-Object { "$($_.Length)`t$($_.Name)" }
} finally {
    $archive.Dispose()
}

[System.IO.File]::WriteAllLines([System.IO.Path]::GetFullPath($Output), $lines)
Write-Host "Wrote $Output ($($lines.Count) files)"
