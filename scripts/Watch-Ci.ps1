# Monitors latest ultrakill-ios-port CI run until completion. Prints STATUS lines for notify.
param(
    [string]$Repo = "m7830380-cyber/ultrakill-ios-port",
    [int]$PollSeconds = 30,
    [int]$MaxMinutes = 45
)

$ErrorActionPreference = "Continue"
$deadline = (Get-Date).AddMinutes($MaxMinutes)
Write-Host "CI_WATCH_START repo=$Repo"

# Wait for a run to appear for tip of main
Start-Sleep -Seconds 8
$runId = $null
while ((Get-Date) -lt $deadline) {
    $json = gh run list --repo $Repo --limit 1 --json databaseId,status,conclusion,displayTitle,url,headSha 2>$null | ConvertFrom-Json
    if ($json) {
        $runId = $json[0].databaseId
        Write-Host ("CI_WATCH_TRACKING id={0} status={1} title={2}" -f $runId, $json[0].status, $json[0].displayTitle)
        break
    }
    Start-Sleep -Seconds 5
}

if (-not $runId) {
    Write-Host "CI_WATCH_FAIL no run found"
    exit 1
}

while ((Get-Date) -lt $deadline) {
    $j = gh run view $runId --repo $Repo --json status,conclusion,url,displayTitle,updatedAt 2>$null | ConvertFrom-Json
    if (-not $j) {
        Start-Sleep -Seconds $PollSeconds
        continue
    }
    # Only emit CI_STATUS occasionally; finals use CI_SUCCESS / CI_FAILED for notify.
    if ($j.status -ne "completed") {
        Write-Host ("CI_PROGRESS status={0}" -f $j.status)
    }
    if ($j.status -eq "completed") {
        if ($j.conclusion -eq "success") {
            $rel = gh release view --repo $Repo --json assets --jq ".assets[] | select(.name|test(\"ipa\";\"i\")) | {updatedAt,size,url:.url}" 2>$null
            Write-Host "CI_SUCCESS url=$($j.url)"
            Write-Host "CI_IPA $rel"
            exit 0
        }
        Write-Host "CI_FAILED conclusion=$($j.conclusion) url=$($j.url)"
        gh run view $runId --repo $Repo --log-failed 2>&1 | Select-String -Pattern "error CS|error:|##\[error\]|Build failed" | Select-Object -First 20 | ForEach-Object { Write-Host "CI_ERR $($_.Line)" }
        exit 1
    }
    Start-Sleep -Seconds $PollSeconds
}

Write-Host "CI_WATCH_TIMEOUT id=$runId"
exit 2
