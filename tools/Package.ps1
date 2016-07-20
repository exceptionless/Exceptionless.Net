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
    If (Test-Path "$($p.SourceDir)\project.json") {
        Write-Host "Building Client NuGet Package: $($p.Name)" -ForegroundColor Yellow
        dotnet pack "$($p.SourceDir)" -c Release -o $artifacts_dir
        Write-Host "Building Client NuGet Package: $($p.Name)" -ForegroundColor Yellow
        
        If (-not $?) {
            $anyError = $True
        }
        
        Continue;
    }
    
    $isSignedProject = $($p.Name).EndsWith(".Signed")
    $workingDirectory = "$working_dir\$($p.Name)"
    Create-Directory $workingDirectory

    Write-Host "Building Client NuGet Package: $($p.Name)" -ForegroundColor Yellow

    #copy assemblies from build directory to working directory.
    ForEach ($b in $client_build_configurations) {
        $buildDirectory = "$build_dir\$configuration\$($p.Name)\lib\$($b.NuGetDir)"
        $workingLibDirectory = "$workingDirectory\lib\$($b.NuGetDir)"
        Create-Directory $workingLibDirectory
        
        Get-ChildItem -Path $buildDirectory | Where-Object { $_.Name -eq "$($p.Name).dll" -Or $_.Name -eq "$($p.Name).pdb" -or $_.Name -eq "$($p.Name).xml" } | Copy-Item -Destination $workingLibDirectory
    }

    # Copy the source code for Symbol Source.
    robocopy $($p.SourceDir) $workingDirectory\src\$($p.SourceDir.Replace($base_dir, """")) *.cs *.xaml /S /NP | Out-Null
    Copy-Item "$base_dir\src\GlobalAssemblyInfo.cs" "$workingDirectory\src\src\GlobalAssemblyInfo.cs"

    If ((Test-Path -Path "$($p.SourceDir)\NuGet")) {
        Copy-Item "$($p.SourceDir)\readme.txt" "$workingDirectory\readme.txt"
        Copy-Item "$($p.SourceDir)\NuGet\*" $workingDirectory -Recurse
    }

    Copy-Item "$($base_dir)\LICENSE.txt" "$workingDirectory"
    
    If ($isSignedProject) {
        Copy-Item "$($source_dir)\Exceptionless.Signed\NuGet\tools\exceptionless.psm1" "$workingDirectory\tools"
    } Else {
        Copy-Item "$($source_dir)\Exceptionless\NuGet\tools\exceptionless.psm1" "$workingDirectory\tools"
    }

    $nuspecFile = "$workingDirectory\$($p.Name).nuspec"
    If ($isSignedProject) {
        $unsignedNuspecFile = $($p.Name).Replace(".Signed", "");
        Rename-Item -Path "$workingDirectory\$unsignedNuspecFile.nuspec" -NewName $nuspecFile
    }

    # update NuGet nuspec file.
    $nuspec = [xml](Get-Content $nuspecFile)
    If (($($p.ExternalNuGetDependencies) -ne $null) -and (Test-Path -Path "$($p.SourceDir)\packages.config")) {
        $packages = [xml](Get-Content "$($p.SourceDir)\packages.config")
            
        ForEach ($d in $($p.ExternalNuGetDependencies).Split(";", [StringSplitOptions]"RemoveEmptyEntries")) {
            $package = $packages.SelectSinglenode("/packages/package[@id=""$d""]")
            $nuspec | Select-Xml -XPath '//dependency' |% {
                If ($_.Node.Id.Equals($d)){
                    $_.Node.Version = "$($package.version)"
                }
            }
        }
    }

    If ($isSignedProject) {
        $nuspec | Select-Xml -XPath '//id' |% { $_.Node.InnerText = $_.Node.InnerText + ".Signed" }
        $nuspec | Select-Xml -XPath '//dependency' |% {
            If($_.Node.Id.StartsWith("Exceptionless")){
                $_.Node.Id = $_.Node.Id + ".Signed"
            }
        }
    }

    $nuspec.Save($nuspecFile);

	$nuget_version = $env:APPVEYOR_BUILD_VERSION + $env:VERSION_SUFFIX
    nuget pack $nuspecFile -OutputDirectory $artifacts_dir -Version $nuget_version -Symbols
}

Pop-Location