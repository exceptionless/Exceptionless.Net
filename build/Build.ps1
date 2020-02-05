Push-Location $PSScriptRoot
. .\Settings.ps1

$anyError = $False

ForEach ($p in $client_projects) {
    Write-Host "Building $($p.Name)" -ForegroundColor Yellow
    dotnet build $($p.SourceDir) -c Release
    Write-Host "Finished building $($p.Name)" -ForegroundColor Yellow

    If ($LASTEXITCODE -ne 0) {
        $anyError = $True
    }
}

Pop-Location

If ($anyError) {
    throw "One or more builds failed"
}