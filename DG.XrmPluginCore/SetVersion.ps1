# Read the contents of CHANGELOG.md
$changelogContent = Get-Content -Path "CHANGELOG.md"

# Extract the first line that starts with "###"
$versionLine = $changelogContent | Select-String -Pattern "^###" | Select-Object -First 1

# Parse the line into VERSION and DATE
if ($versionLine -match "^###\s+(\S+)\s+-\s+(\S+.*)$") {
    $version = $matches[1]
    $date = $matches[2]
    Write-Output "VERSION: $version"
    Write-Output "DATE: $date"

    # Write the version and date to the version file
    Set-Content -Path "VERSION" -Value $version
} else {
    Write-Output "No matching version line found."
}
