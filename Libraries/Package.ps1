Include .\Settings.ps1

Function Create-Directory([string] $directory_name) {
    If (!(Test-Path -Path $directory_name)) {
        New-Item $directory_name -ItemType Directory | Out-Null
    }
}

Create-Directory $deploy_dir

ForEach ($p in $client_projects) {
    $isSignedProject = $($p.Name).EndsWith(".Signed")
    $assemblyName = $($p.Name).Replace(".Signed", "")
    $workingDirectory = "$working_dir\$($p.Name)"
    Create-Directory $workingDirectory

    Write-Host "Building Client NuGet Package: $($p.Name)"

    #copy assemblies from build directory to working directory.
    ForEach ($b in $client_build_configurations) {
        $isPclClient = ($($p.Name) -eq "Exceptionless") -or ($($p.Name) -eq "Exceptionless.Signed")
        If (($isPclClient -and ($($b.NuGetDir) -ne "portable-net40+sl50+win+wpa81+wp80")) -or (!$isPclClient -and ($($b.NuGetDir) -eq "portable-net40+sl50+win+wpa81+wp80"))) {
            Continue;
        }

        $buildDirectory = "$build_dir\$configuration\$($p.Name)\lib\$($b.NuGetDir)"
        $workingLibDirectory = "$workingDirectory\lib\$($b.NuGetDir)"
        Create-Directory $workingLibDirectory

        # Work around until we are able to merge dependencies and update other project dependencies pre build (E.G., MVC client references Models)
        Get-ChildItem -Path $buildDirectory | Where-Object { $_.Name -eq "$assemblyName.dll" -Or $_.Name -eq "$assemblyName.pdb" -or $_.Name -eq "$assemblyName.xml" } | Copy-Item -Destination $workingLibDirectory

        If ($($p.MergeDependencies) -ne $null) {
            ForEach ($assembly in $($p.MergeDependencies).Split(";", [StringSplitOptions]"RemoveEmptyEntries")) {
                Get-ChildItem -Path $buildDirectory | Where-Object { $_.Name -eq "$assembly" -Or $_.Name -eq "$assembly".Replace(".dll", ".pdb") -or $_.Name -eq "$assembly".Replace(".dll", ".xml") } | Copy-Item -Destination $workingLibDirectory
            }
        }
    }

    # Copy the source code for Symbol Source.
    robocopy $($p.SourceDir) $workingDirectory\src\$($p.SourceDir.Replace($base_dir, """")) *.cs *.xaml /S /NP
    robocopy "$base_dir\Source\Core" "$workingDirectory\src\Source\Core" *.cs /S /NP /XD obj
    Copy-Item "$base_dir\Source\GlobalAssemblyInfo.cs" "$workingDirectory\src\Source\GlobalAssemblyInfo.cs"

    If (($($p.Name) -ne "Exceptionless") -and ($($p.Name) -ne "Exceptionless.Signed")) {
        robocopy "$base_dir\Source\Extras" "$workingDirectory\src\Source\Extras" *.cs /S /NP /XD obj
    }

    If ($($p.Name).StartsWith("Exceptionless.Mvc")) {
        robocopy "$base_dir\Source\Platforms\Web" "$workingDirectory\src\Source\Platforms\Web" *.cs /S /NP /XD obj
    }

    If ((Test-Path -Path "$($p.SourceDir)\NuGet")) {
        Copy-Item "$($p.SourceDir)\NuGet\*" $workingDirectory -Recurse
    }

    Copy-Item "$($source_dir)\Platforms\LICENSE.txt" "$workingDirectory"
    Copy-Item "$($source_dir)\Shared\NuGet\tools\exceptionless.psm1" "$workingDirectory\tools"

    $nuspecFile = "$workingDirectory\$($p.Name).nuspec"
    If ($isSignedProject){
        Rename-Item -Path "$workingDirectory\$assemblyName.nuspec" -NewName $nuspecFile
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

    $packageDir = "$deploy_dir\packages"
    Create-Directory $packageDir

    exec { & nuget pack $nuspecFile -OutputDirectory $packageDir -Version "$env:APPVEYOR_BUILD_VERSION" -Symbols }
}

Get-ChildItem .\*.nupkg | % { Push-AppveyorArtifact $_.FullName -FileName $_.Name }