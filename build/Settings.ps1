$configuration = "Release"
$base_dir = Resolve-Path "..\"
$artifacts_dir = "$base_dir\artifacts"
$build_dir = "$base_dir\build"
$source_dir = "$base_dir\src"
$working_dir = "$build_dir\working"
$sign_file = "$source_dir\Exceptionless.snk"

$client_projects = @(
    @{ Name = "Exceptionless";                     SourceDir = "$source_dir\Exceptionless";                              ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Signed";              SourceDir = "$source_dir\Exceptionless.Signed";                       ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Portable";            SourceDir = "$source_dir\Exceptionless.Portable";                     ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Portable.Signed";     SourceDir = "$source_dir\Exceptionless.Portable.Signed";              ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.AspNetCore";          SourceDir = "$source_dir\Platforms\Exceptionless.AspNetCore";         ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.AspNetCore.Signed";   SourceDir = "$source_dir\Platforms\Exceptionless.AspNetCore.Signed";  ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Extensions.Logging";  SourceDir = "$source_dir\Platforms\Exceptionless.Extensions.Logging"; ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Mvc";                 SourceDir = "$source_dir\Platforms\Exceptionless.Mvc";                ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Mvc.Signed";          SourceDir = "$source_dir\Platforms\Exceptionless.Mvc.Signed";         ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Nancy";               SourceDir = "$source_dir\Platforms\Exceptionless.Nancy";              ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.WebApi";              SourceDir = "$source_dir\Platforms\Exceptionless.WebApi";             ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.WebApi.Signed";       SourceDir = "$source_dir\Platforms\Exceptionless.WebApi.Signed";      ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Web";                 SourceDir = "$source_dir\Platforms\Exceptionless.Web";                ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Web.Signed";          SourceDir = "$source_dir\Platforms\Exceptionless.Web.Signed";         ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.NLog";                SourceDir = "$source_dir\Platforms\Exceptionless.NLog";               ExternalNuGetDependencies = "NLog"; },
    @{ Name = "Exceptionless.NLog.Signed";         SourceDir = "$source_dir\Platforms\Exceptionless.NLog.Signed";        ExternalNuGetDependencies = "NLog"; },
    @{ Name = "Exceptionless.Log4net";             SourceDir = "$source_dir\Platforms\Exceptionless.Log4net";            ExternalNuGetDependencies = "log4net"; },
    @{ Name = "Exceptionless.Log4net.Signed";      SourceDir = "$source_dir\Platforms\Exceptionless.Log4net.Signed";     ExternalNuGetDependencies = "log4net"; },
    @{ Name = "Exceptionless.MessagePack";         SourceDir = "$source_dir\Platforms\Exceptionless.MessagePack";        ExternalNuGetDependencies = "MessagePack"; },
    @{ Name = "Exceptionless.MessagePack.Signed";  SourceDir = "$source_dir\Platforms\Exceptionless.MessagePack.Signed"; ExternalNuGetDependencies = "MessagePack"; },

    @{ Name = "Exceptionless.Windows";             SourceDir = "$source_dir\Platforms\Exceptionless.Windows";            ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Windows.Signed";      SourceDir = "$source_dir\Platforms\Exceptionless.Windows.Signed";     ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Wpf";                 SourceDir = "$source_dir\Platforms\Exceptionless.Wpf";                ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Wpf.Signed";          SourceDir = "$source_dir\Platforms\Exceptionless.Wpf.Signed";         ExternalNuGetDependencies = $null; }
)