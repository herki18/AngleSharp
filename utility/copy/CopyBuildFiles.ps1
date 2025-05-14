param (
    [Parameter(Mandatory = $true)]
    [string]$ConfigFile
)

if (!(Test-Path $ConfigFile)) {
    Write-Error "Config file not found: $ConfigFile"
    exit 1
}

try {
    $config = Get-Content $ConfigFile -Raw | ConvertFrom-Json
} catch {
    Write-Error "Failed to parse config file: $_"
    exit 1
}

foreach ($item in $config) {
    $src = $item.Source
    $dst = $item.Destination

    if (-not (Test-Path $src)) {
        Write-Warning "Source does not exist: $src"
        continue
    }

    if (-not (Test-Path $dst)) {
        try {
            New-Item -ItemType Directory -Path $dst -Force | Out-Null
            Write-Host "Created destination directory: $dst"
        } catch {
            Write-Warning "Failed to create destination: $dst"
            continue
        }
    }

    try {
        # Get all files to be copied (recursively)
        $files = Get-ChildItem -Path $src -File -Recurse
        $fileCount = $files.Count

        Copy-Item -Path "$src\*" -Destination $dst -Recurse -Force

        Write-Host "Copied $fileCount file(s) from $src to $dst"
    } catch {
        Write-Warning ("Failed to copy from {0} to {1}: {2}" -f $src, $dst, $_)

    }
}
