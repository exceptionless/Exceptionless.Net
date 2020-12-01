# Pull sources
if (Test-Path json.zip) {
	del json.zip
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest https://github.com/JamesNK/Newtonsoft.Json/archive/12.0.3.zip -OutFile json.zip

if (Test-Path json-temp) {
	rmdir './json-temp' -Recurse -Force
}
[System.Reflection.Assembly]::LoadWithPartialName("System.IO.Compression.FileSystem") | Out-Null
[System.IO.Compression.ZipFile]::ExtractToDirectory($pwd.Path + "/json.zip", $pwd.Path + "/json-temp")

if (Test-Path Newtonsoft.Json) {
	rmdir './Newtonsoft.Json' -Recurse -Force
}

cd 'json-temp/Newtonsoft.Json*'
Copy-Item 'Src/Newtonsoft.Json' -Destination '../../' -Recurse
cd '../../'

rmdir './json-temp' -Recurse -Force
del json.zip

Get-ChildItem './Newtonsoft.Json' *.cs -recurse |
    Foreach-Object {
        $c = ($_ | Get-Content) 
        $c = $c -replace 'Newtonsoft.Json','Exceptionless.Json'
        $c = $c -replace 'JsonIgnoreAttribute','ExceptionlessIgnoreAttribute'
        if($_.name -ne 'JsonIgnoreAttribute.cs'){
            $c = $c -replace 'public( (?:static|sealed|abstract|partial))? (class|struct|interface|enum)','internal$1 $2'
            $c = $c -replace 'public delegate void','internal delegate void'
            $c = $c -replace '\[CLSCompliant\(false\)\]',''
        }
        $c | Set-Content $_.FullName
    }

Rename-Item -Path "./Newtonsoft.Json/JsonIgnoreAttribute.cs" "ExceptionlessIgnoreAttribute.cs"

del './Newtonsoft.Json/*.csproj' -Force
del './Newtonsoft.Json/Newtonsoft.Json.ruleset' -Force
del './Newtonsoft.Json/Properties' -Force -Recurse
