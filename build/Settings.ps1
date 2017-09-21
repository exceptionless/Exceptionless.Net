$configuration = "Release"
$base_dir = Resolve-Path "..\"
$artifacts_dir = "$base_dir\artifacts"
$build_dir = "$base_dir\build"
$source_dir = "$base_dir\src"
$working_dir = "$build_dir\working"
$sign_file = "$source_dir\Exceptionless.snk"

$client_projects = @(
    @{ Name = "Exceptionless";                     SourceDir = "$source_dir\Exceptionless";                              ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Signed";              SourceDir = "$source_dir\Exceptionless.Signed";                       ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Portable";            SourceDir = "$source_dir\Exceptionless.Portable";                     ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Portable.Signed";     SourceDir = "$source_dir\Exceptionless.Portable.Signed";              ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.AspNetCore";          SourceDir = "$source_dir\Platforms\Exceptionless.AspNetCore";         ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.AspNetCore.Signed";   SourceDir = "$source_dir\Platforms\Exceptionless.AspNetCore.Signed";  ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Extensions.Logging";  SourceDir = "$source_dir\Platforms\Exceptionless.Extensions.Logging"; ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Mvc";                 SourceDir = "$source_dir\Platforms\Exceptionless.Mvc";                ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Mvc.Signed";          SourceDir = "$source_dir\Platforms\Exceptionless.Mvc.Signed";         ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Nancy";               SourceDir = "$source_dir\Platforms\Exceptionless.Nancy";              ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.WebApi";              SourceDir = "$source_dir\Platforms\Exceptionless.WebApi";             ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.WebApi.Signed";       SourceDir = "$source_dir\Platforms\Exceptionless.WebApi.Signed";      ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Web";                 SourceDir = "$source_dir\Platforms\Exceptionless.Web";                ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.Web.Signed";          SourceDir = "$source_dir\Platforms\Exceptionless.Web.Signed";         ExternalNuGetDependencies = $null;      UseMSBuild = $False; },
    @{ Name = "Exceptionless.NLog";                SourceDir = "$source_dir\Platforms\Exceptionless.NLog";               ExternalNuGetDependencies = "NLog";     UseMSBuild = $False; },
    @{ Name = "Exceptionless.NLog.Signed";         SourceDir = "$source_dir\Platforms\Exceptionless.NLog.Signed";        ExternalNuGetDependencies = "NLog";     UseMSBuild = $False; },
    @{ Name = "Exceptionless.Log4net";             SourceDir = "$source_dir\Platforms\Exceptionless.Log4net";            ExternalNuGetDependencies = "log4net";  UseMSBuild = $False; },
    @{ Name = "Exceptionless.Log4net.Signed";      SourceDir = "$source_dir\Platforms\Exceptionless.Log4net.Signed";     ExternalNuGetDependencies = "log4net";  UseMSBuild = $False; },

    @{ Name = "Exceptionless.Windows";             SourceDir = "$source_dir\Platforms\Exceptionless.Windows";            ExternalNuGetDependencies = $null;      UseMSBuild = $True; },
    @{ Name = "Exceptionless.Windows.Signed";      SourceDir = "$source_dir\Platforms\Exceptionless.Windows";            ExternalNuGetDependencies = $null;      UseMSBuild = $True; },
    @{ Name = "Exceptionless.Wpf";                 SourceDir = "$source_dir\Platforms\Exceptionless.Wpf";                ExternalNuGetDependencies = $null;      UseMSBuild = $True; },
    @{ Name = "Exceptionless.Wpf.Signed";          SourceDir = "$source_dir\Platforms\Exceptionless.Wpf";                ExternalNuGetDependencies = $null;      UseMSBuild = $True; }
)

$client_build_configurations = @(
    @{ Constants = "NET45"; TargetFrameworkVersionProperty="NET45";   NuGetDir = "net45"; }
)