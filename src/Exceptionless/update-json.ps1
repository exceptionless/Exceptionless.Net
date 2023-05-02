# Pull sources
if (Test-Path json.zip) {
	Remove-Item json.zip
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest https://github.com/JamesNK/Newtonsoft.Json/archive/13.0.3.zip -OutFile json.zip

if (Test-Path json-temp) {
	Remove-Item './json-temp' -Recurse -Force
}
[System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null
[System.IO.Compression.ZipFile]::ExtractToDirectory($pwd.Path + "/json.zip", $pwd.Path + "/json-temp")

if (Test-Path Newtonsoft.Json) {
	Remove-Item './Newtonsoft.Json' -Recurse -Force
}

Set-Location 'json-temp/Newtonsoft.Json*'
Copy-Item 'Src/Newtonsoft.Json' -Destination '../../' -Recurse
Set-Location '../../'

Remove-Item './json-temp' -Recurse -Force -ErrorAction SilentlyContinue *> $null
Remove-Item json.zip
Remove-Item './json-temp' -Recurse -Force -ErrorAction SilentlyContinue *> $null

Get-ChildItem './Newtonsoft.Json' *.cs -recurse |
    Foreach-Object {
        $c = ($_ | Get-Content)
        $c = $c -replace 'Newtonsoft.Json','Exceptionless.Json'
        $c = $c -replace 'JsonIgnoreAttribute','ExceptionlessIgnoreAttribute'
        if($_.name -ne 'JsonIgnoreAttribute.cs'){
            $c = $c -replace 'public((?: (?:readonly|static|sealed|abstract|partial))+)? (class|struct|interface|enum)','internal$1 $2'
            $c = $c -replace 'public delegate void','internal delegate void'
            $c = $c -replace '\[CLSCompliant\(false\)\]',''
        }
        $c | Set-Content $_.FullName
    }

Rename-Item -Path "./Newtonsoft.Json/JsonIgnoreAttribute.cs" "ExceptionlessIgnoreAttribute.cs"

Remove-Item './Newtonsoft.Json/*.csproj' -Force
Remove-Item './Newtonsoft.Json/CompatibilitySuppressions.xml' -Force -Recurse
Remove-Item './Newtonsoft.Json/Newtonsoft.Json.ruleset' -Force
Remove-Item './Newtonsoft.Json/Properties' -Force -Recurse
Remove-Item './Newtonsoft.Json/packageIcon.png' -Force -Recurse
Remove-Item './Newtonsoft.Json/README.md' -Force -Recurse
