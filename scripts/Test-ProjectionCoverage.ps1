param(
    [string]$ReportDirectory = (Join-Path $PSScriptRoot '../artifacts/projection-coverage-net48'),
    [ValidateSet('net6.0', 'net48')][string]$Framework = 'net48',
    [double]$Minimum = 90
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content -LiteralPath (Join-Path $repo 'test/projection-coverage-manifest.json') -Raw | ConvertFrom-Json
$expected = @{}
foreach ($entry in $manifest) {
    if (!(Test-Path -LiteralPath (Join-Path $repo $entry.path))) { throw "Missing manifest source: $($entry.path)" }
    if (!$entry.executable) { continue }
    $parts = $entry.path.Split('/')
    $name = if ($parts[1] -eq 'Rendering') { $parts[2] } else { $parts[1] }
    if ($name -eq 'Expresso.Rendering.EntityFramework' -and $env:OS -ne 'Windows_NT') { continue }
    if (!$expected.ContainsKey($name)) { $expected[$name] = @{} }
    $expected[$name][$parts[-1]] = $entry.path
}
# TRX attachments identify the instrumented test framework. Never merge net6 source
# branches from an EF Core/net10 host into the netstandard2.0 coverage path.
$reportFrameworks = @{}
foreach ($trx in @(Get-ChildItem -LiteralPath $ReportDirectory -Filter '*.trx' -Recurse)) {
    [xml]$run = Get-Content -LiteralPath $trx.FullName -Raw
    $test = $run.SelectSingleNode("//*[local-name()='UnitTest']")
    if ($null -eq $test) { continue }
    $storage = ([string]$test.storage).Replace('\', '/')
    if ($storage -notmatch '/(net[^/]+)/(?:[^/]+/)?[^/]+\.dll$') { throw "Cannot identify test framework: $($trx.FullName)" }
    $testFramework = $Matches[1]
    $deployment = $run.SelectSingleNode("//*[local-name()='Deployment']").GetAttribute('runDeploymentRoot')
    foreach ($attachment in $run.SelectNodes("//*[local-name()='UriAttachment']/*[local-name()='A']")) {
        $uri = $attachment.GetAttribute('href').Replace('\', '/')
        if ($uri -notmatch 'coverage\.cobertura\.xml$') { continue }
        $reportPath = [IO.Path]::GetFullPath((Join-Path $trx.Directory.FullName (Join-Path $deployment (Join-Path 'In' $uri))))
        if (!(Test-Path -LiteralPath $reportPath)) { throw "Missing TRX coverage attachment: $reportPath" }
        $reportFrameworks[$reportPath] = $testFramework
    }
}
$reports = @($reportFrameworks.Keys | ForEach-Object { Get-Item -LiteralPath $_ })
if ($reports.Count -eq 0) { throw "No Cobertura reports in $ReportDirectory." }
$packages = @{}
$observed = @{}
foreach ($report in $reports) {
    $id = $report.FullName
    [xml]$xml = Get-Content -LiteralPath $report.FullName -Raw
    foreach ($package in $xml.coverage.packages.package) {
        $name = [string]$package.name
        if (!$expected.ContainsKey($name)) { continue }
        $target = if ($name -eq 'Expresso.Rendering.EntityFrameworkCore') { 'net8.0' } elseif ($name -eq 'Expresso.Rendering.EntityFramework') { 'net48' } else { $Framework }
        if ($reportFrameworks[$id] -ne $target) { continue }
        if (!$packages.ContainsKey($name)) { $packages[$name] = @{}; $observed[$name] = @{} }
        foreach ($class in $package.classes.class) {
            $file = ([string]$class.filename).Replace('\', '/').Split('/')[-1]
            if (!$expected[$name].ContainsKey($file)) { continue }
            $observed[$name][$file] = $true
            foreach ($line in $class.lines.line) {
                $key = $file + ':' + $line.number
                $covered = 0; $total = 0
                if ([string]$line.'condition-coverage' -match '\((\d+)/(\d+)\)') {
                    $covered = [int]$Matches[1]; $total = [int]$Matches[2]
                }
                if (!$packages[$name].ContainsKey($key)) { $packages[$name][$key] = @{ Hit = 0; Covered = 0; Total = 0 } }
                $entry = $packages[$name][$key]
                $entry.Hit = [Math]::Max($entry.Hit, [int]$line.hits)
                $entry.Covered = [Math]::Max($entry.Covered, $covered)
                $entry.Total = [Math]::Max($entry.Total, $total)
            }
        }
    }
}
$failed = $false
foreach ($name in @($expected.Keys | Sort-Object)) {
    if (!$packages.ContainsKey($name) -or $packages[$name].Count -eq 0) {
        Write-Output "$name : MISSING $Framework-path coverage"; $failed = $true; continue
    }
    foreach ($file in $expected[$name].Keys) {
        if (!$observed[$name].ContainsKey($file)) { Write-Output "$name : MISSING manifest file $file"; $failed = $true }
    }
    $lines = @($packages[$name].Values)
    $hit = @($lines | Where-Object { $_.Hit -gt 0 }).Count
    $branches = 0; $covered = 0
    foreach ($entry in $lines) { $branches += [int]$entry.Total; $covered += [int]$entry.Covered }
    $lineRate = 100 * $hit / $lines.Count
    $branchRate = if ($branches -eq 0) { 100 } else { 100 * $covered / $branches }
    Write-Output ('{0} ({1} path): line {2:N2}% ({3}/{4}); branch {5:N2}% ({6}/{7})' -f $name, $Framework, $lineRate, $hit, $lines.Count, $branchRate, $covered, $branches)
    if ($lineRate -lt $Minimum -or $branchRate -lt $Minimum) { $failed = $true }
}
if ($failed) { throw "Projection coverage must reach $Minimum% line and branch coverage in every updated library and include every executable manifest file." }
