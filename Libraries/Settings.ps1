$configuration = "Release"
$base_dir = Resolve-Path "..\"
$build_dir = "$base_dir\Build"
$deploy_dir = "$build_dir\Deploy"
$working_dir = "$build_dir\Working"
$source_dir = "$base_dir\Source"
$sign_file = "$source_dir\Exceptionless.snk"

$client_projects = @(
    @{ Name = "Exceptionless.Portable"; 		SourceDir = "$source_dir\Shared";				ExternalNuGetDependencies = $null;		MergeDependencies = $null; },
    @{ Name = "Exceptionless.Portable.Signed"; 	SourceDir = "$source_dir\Shared";				ExternalNuGetDependencies = $null;		MergeDependencies = $null; },
    @{ Name = "Exceptionless.Mvc";  			SourceDir = "$source_dir\Platforms\Mvc"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Mvc.Signed";  		SourceDir = "$source_dir\Platforms\Mvc"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Nancy";  			SourceDir = "$source_dir\Platforms\Nancy"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.WebApi";  			SourceDir = "$source_dir\Platforms\WebApi"; 	ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.WebApi.Signed";  	SourceDir = "$source_dir\Platforms\WebApi"; 	ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Web"; 				SourceDir = "$source_dir\Platforms\Web"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Web.Signed"; 		SourceDir = "$source_dir\Platforms\Web"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Windows"; 			SourceDir = "$source_dir\Platforms\Windows"; 	ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Windows.Signed"; 	SourceDir = "$source_dir\Platforms\Windows"; 	ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless"; 					SourceDir = "$source_dir\Platforms\Console"; 	ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Signed"; 			SourceDir = "$source_dir\Platforms\Console"; 	ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Wpf"; 				SourceDir = "$source_dir\Platforms\Wpf"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Wpf.Signed"; 		SourceDir = "$source_dir\Platforms\Wpf"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.NLog"; 			SourceDir = "$source_dir\Platforms\NLog";		ExternalNuGetDependencies = "NLog";		MergeDependencies = $null; }
    @{ Name = "Exceptionless.NLog.Signed"; 		SourceDir = "$source_dir\Platforms\NLog";		ExternalNuGetDependencies = "NLog";		MergeDependencies = $null; }
)

$client_build_configurations = @(
    @{ Constants = "PORTABLE40";	TargetFrameworkVersionProperty="NET40";	NuGetDir = "portable-net40+sl50+win+wpa81+wp80"; }
    @{ Constants = "PORTABLE40"; 	TargetFrameworkVersionProperty="NET40";	NuGetDir = "net40"; },
    @{ Constants = "PORTABLE40"; 	TargetFrameworkVersionProperty="NET45";	NuGetDir = "net45"; }
)