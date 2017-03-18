Push-Location $PSScriptRoot
. .\Settings.ps1

$anyError = $False

ForEach ($p in $client_projects) {
    If ($($p.UseMSBuild) -ne $True) {
        Write-Host "Building $($p.Name)" -ForegroundColor Yellow
        If ($($p.Name).EndsWith(".Signed")) {
            dotnet pack $($p.SourceDir) -c Release -o $artifacts_dir /p:SignAssembly=true
        } Else {
            dotnet pack $($p.SourceDir) -c Release -o $artifacts_dir
        }
        Write-Host "Finished building $($p.Name)" -ForegroundColor Yellow

        If ($LASTEXITCODE -ne 0) {
            $anyError = $True
        }
    } Else {
        ForEach ($b in $client_build_configurations) {
            $outputDirectory = "$build_dir\$configuration\$($p.Name)\lib\$($b.NuGetDir)"
            Write-Host "Building $($p.Name) ($($b.TargetFrameworkVersionProperty))" -ForegroundColor Yellow

            If ($($p.Name).EndsWith(".Signed")) {
                $name = $($p.Name).Replace(".Signed", "");
                msbuild "$($p.SourceDir)\$name.csproj" `
                            /p:AssemblyName="$($p.Name)" `
                            /p:DocumentationFile="bin\$($configuration)\$($b.TargetFrameworkVersionProperty)\$($p.Name).xml" `
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
            } Else {
                msbuild "$($p.SourceDir)\$($p.Name).csproj" `
                            /p:AssemblyName="$($p.Name)" `
                            /p:DocumentationFile="bin\$($configuration)\$($b.TargetFrameworkVersionProperty)\$($p.Name).xml" `
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
            If ($LASTEXITCODE -ne 0) {
                $anyError = $True
            }

            Write-Host "Finished building $($p.Name) ($($b.TargetFrameworkVersionProperty))" -ForegroundColor Yellow
        }
    }
}

Pop-Location

If ($anyError) {
    throw "One or more builds failed"
}