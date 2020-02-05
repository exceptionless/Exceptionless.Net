Push-Location $PSScriptRoot
. .\Settings.ps1

Function Create-Directory([string] $directory_name) {
    If (Test-Path -Path $directory_name) {
        Remove-Item -Recurse -Force -Path $directory_name | Out-Null
    }
    
    If (!(Test-Path -Path $directory_name)) {
        New-Item $directory_name -ItemType Directory | Out-Null
    }
}

Create-Directory $artifacts_dir

ForEach ($p in $client_projects) {
    Write-Host "Building Client NuGet Package: $($p.Name)" -ForegroundColor Yellow
    dotnet pack "$($p.SourceDir)" -c Release -o $artifacts_dir
    Write-Host "Building Client NuGet Package: $($p.Name)" -ForegroundColor Yellow
    
    If (-not $?) {
        $anyError = $True
    }
}

Pop-Location