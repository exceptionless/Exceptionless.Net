Push-Location $PSScriptRoot
. .\Settings.ps1

ForEach ($p in $client_projects) {
    ForEach ($b in $client_build_configurations) {
        $isPclClient = ($($p.Name) -eq "Exceptionless.Portable") -or ($($p.Name) -eq "Exceptionless.Portable.Signed")
        If (($isPclClient -and ($($b.NuGetDir) -ne "portable-net40+sl50+win+wpa81+wp80")) -or (!$isPclClient -and ($($b.NuGetDir) -eq "portable-net40+sl50+win+wpa81+wp80"))) {
            Continue;
        }

        $outputDirectory = "$build_dir\$configuration\$($p.Name)\lib\$($b.NuGetDir)"

        Write-Host "Building $($p.Name) ($($b.TargetFrameworkVersionProperty))" -ForegroundColor Yellow
        
        If ($($p.Name).EndsWith(".Signed")) {
            $name = $($p.Name).Replace(".Signed", "");
            msbuild "$($p.SourceDir)\$name.csproj" `
                        /p:SignAssembly=true `
                        /p:AssemblyOriginatorKeyFile="$sign_file" `
                        /p:Configuration="$configuration" `
                        /p:Platform="AnyCPU" `
                        /p:NoWarn="1591 1711 1712 1572 1573 1574" `
                        /verbosity:minimal `
                        /p:DefineConstants="`"TRACE;SIGNED;$($b.Constants)`"" `
                        /p:OutputPath="$outputDirectory" `
                        /p:TargetFrameworkVersionProperty="$($b.TargetFrameworkVersionProperty)" `
                        /t:"Rebuild"
        } else {
            msbuild "$($p.SourceDir)\$($p.Name).csproj" `
                        /p:SignAssembly=false `
                        /p:Configuration="$configuration" `
                        /p:Platform="AnyCPU" `
                        /p:NoWarn="1591 1711 1712 1572 1573 1574" `
                        /verbosity:minimal `
                        /p:DefineConstants="`"TRACE;$($b.Constants)`"" `
                        /p:OutputPath="$outputDirectory" `
                        /p:TargetFrameworkVersionProperty="$($b.TargetFrameworkVersionProperty)" `
                        /t:"Rebuild"
        }

        Write-Host "Finished building $($p.Name) ($($b.TargetFrameworkVersionProperty))" -ForegroundColor Yellow
    }
}

Write-Host "Building Client Tests" -ForegroundColor Yellow

msbuild "$source_dir\Tests\Exceptionless.Tests.csproj" /p:Configuration="$configuration" /t:Rebuild /p:NoWarn="1591 1711 1712 1572 1573 1574" /verbosity:minimal

Write-Host "Finished building Client Tests" -ForegroundColor Yellow

Pop-Location