# Simple release notes generator for Jenkins Agent (PowerShell)
# Usage: .\Scripts\generate_release_notes.ps1 v1.2.3

param(
    [Parameter(Mandatory = $true)]
    [string]$Tag
)

# Find previous tag (if any)
$tags = git tag --sort=-creatordate
$tagList = $tags -split "`n" | Where-Object { $_ -ne '' }
$tagIndex = $tagList.IndexOf($Tag)
if ($tagIndex -eq -1) {
    Write-Error "Tag '$Tag' not found."
    exit 1
}
if ($tagIndex -eq 0) {
    $prevTag = (git rev-list --max-parents=0 HEAD).Trim()
    $title = "Initial release"
} else {
    $prevTag = $tagList[$tagIndex - 1]
    $title = "Changes since $prevTag"
}
$range = "$prevTag..$Tag"

# Release date
$releaseDate = git log -1 --format=%ai $Tag | ForEach-Object { $_.Split(' ')[0] }

Write-Output "## 📋 $title"
Write-Output ""
Write-Output "**Release Date:** $releaseDate"
Write-Output ""

Write-Output "## 🚀 Highlights"
$highlights = git log --pretty=format:"- %s" $range | Select-String -NotMatch "Merge pull|bump version|ci:|chore:|docs:|test:"
$highlights | Select-Object -First 20 | ForEach-Object { $_.ToString() } | Write-Output
Write-Output ""

Write-Output "## 📝 Full Changelog"
if ($tagIndex -ne 0) {
    Write-Output "[Compare changes](https://github.com/mustafagenc/jenkins-agent/compare/$prevTag...$Tag)"
} else {
    Write-Output "[Commits](https://github.com/mustafagenc/jenkins-agent/commits/$Tag)"
}
Write-Output ""

Write-Output "---"
Write-Output "### Kurulum"
Write-Output "- Son sürüm .exe dosyasını [Releases](https://github.com/mustafagenc/jenkins-agent/releases) sayfasından indirin."
Write-Output "- Kurulum için: İndirilen JenkinsAgentSetup_$Tag.exe dosyasını çalıştırın."
Write-Output "- Geliştiriciler için:"
Write-Output '  ```bash'
Write-Output '  git clone https://github.com/mustafagenc/jenkins-agent.git'
Write-Output '  cd jenkins-agent'
Write-Output '  dotnet restore && dotnet build -c Release'
Write-Output '  cd bin/Release/net9.0-windows'
Write-Output '  JenkinsAgent.exe'
Write-Output '  ```'
Write-Output ""
Write-Output "---"
Write-Output "[Dökümantasyon](https://github.com/mustafagenc/jenkins-agent#readme) | [Sorun Bildir](https://github.com/mustafagenc/jenkins-agent/issues)"
