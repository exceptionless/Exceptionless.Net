Push-Location $PSScriptRoot
. .\Settings.ps1

$anyError = $False

ForEach ($p in $client_projects) {
    If (Test-Path "$($p.SourceDir)\project.json") {
        Write-Host "Building $($p.Name)" -ForegroundColor Yellow
        dotnet build "$($p.SourceDir)" -c Release
        Write-Host "Finished building $($p.Name)" -ForegroundColor Yellow
        
        If (-not $?) {
            $anyError = $True
        }
        
        Continue;
    }
    
    ForEach ($b in $client_build_configurations) {
        $outputDirectory = "$build_dir\$configuration\$($p.Name)\lib\$($b.NuGetDir)"
        Write-Host "Building $($p.Name) ($($b.TargetFrameworkVersionProperty))" -ForegroundColor Yellow
        
        If ($($p.Name).EndsWith(".Signed")) {
            $name = $($p.Name).Replace(".Signed", "");
            msbuild "$($p.SourceDir)\$name.csproj" `
                        /p:SignAssembly=true `
                        /p:AssemblyOriginatorKeyFile="$sign_file" `
                        /p:Configuration="$configuration" `
                        /p:Platform="AnyCPU" `
                        /p:NoWarn="1591" `
                        /verbosity:minimal `
                        /p:DefineConstants="`"TRACE;SIGNED;$($b.Constants)`"" `
                        /p:OutputPath="$outputDirectory" `
                        /p:TargetPortable="$targetPortable" `
                        /p:TargetFrameworkVersionProperty="$($b.TargetFrameworkVersionProperty)" `
                        /t:"Rebuild"
        } else {
            msbuild "$($p.SourceDir)\$($p.Name).csproj" `
                        /p:SignAssembly=false `
                        /p:Configuration="$configuration" `
                        /p:Platform="AnyCPU" `
                        /p:NoWarn="1591" `
                        /verbosity:minimal `
                        /p:DefineConstants="`"TRACE;$($b.Constants)`"" `
                        /p:OutputPath="$outputDirectory" `
                        /p:TargetPortable="$targetPortable" `
                        /p:TargetFrameworkVersionProperty="$($b.TargetFrameworkVersionProperty)" `
                        /t:"Rebuild"
        }
		If (-not $?) {
            $anyError = $True
        }

        Write-Host "Finished building $($p.Name) ($($b.TargetFrameworkVersionProperty))" -ForegroundColor Yellow
    }
}

Pop-Location

If ($anyError) {
	exit 1
}