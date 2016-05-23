$configuration = "Release"
$base_dir = Resolve-Path "..\"
$build_dir = "$base_dir\Build"
$deploy_dir = "$build_dir\Deploy"
$working_dir = "$build_dir\Working"
$source_dir = "$base_dir\src"
$sign_file = "$source_dir\Exceptionless.snk"

$client_projects = @(
    @{ Name = "Exceptionless.Portable"; 		SourceDir = "$source_dir\Exceptionless.Portable";			        ExternalNuGetDependencies = $null;		MergeDependencies = $null; },
    @{ Name = "Exceptionless.Portable.Signed"; 	SourceDir = "$source_dir\Exceptionless.Portable.Signed";			ExternalNuGetDependencies = $null;		MergeDependencies = $null; },
    @{ Name = "Exceptionless.Mvc";  			SourceDir = "$source_dir\Platforms\Exceptionless.Mvc"; 		        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Mvc.Signed";  		SourceDir = "$source_dir\Platforms\Exceptionless.Mvc.Signed"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Nancy";  			SourceDir = "$source_dir\Platforms\Exceptionless.Nancy"; 	        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.WebApi";  			SourceDir = "$source_dir\Platforms\Exceptionless.WebApi"; 	        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.WebApi.Signed";  	SourceDir = "$source_dir\Platforms\Exceptionless.WebApi.Signed"; 	ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Web"; 				SourceDir = "$source_dir\Platforms\Exceptionless.Web"; 		        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Web.Signed"; 		SourceDir = "$source_dir\Platforms\Exceptionless.Web.Signed"; 		ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Windows"; 			SourceDir = "$source_dir\Platforms\Exceptionless.Windows"; 	        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Windows.Signed"; 	SourceDir = "$source_dir\Platforms\Exceptionless.Windows"; 	        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless"; 					SourceDir = "$source_dir\Platforms\Exceptionless"; 	                ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Signed"; 			SourceDir = "$source_dir\Platforms\Exceptionless.Signed"; 	        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Wpf"; 				SourceDir = "$source_dir\Platforms\Exceptionless.Wpf"; 		        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.Wpf.Signed"; 		SourceDir = "$source_dir\Platforms\Exceptionless.Wpf"; 		        ExternalNuGetDependencies = $null;		MergeDependencies = "Exceptionless.Extras.dll;"; },
    @{ Name = "Exceptionless.NLog"; 			SourceDir = "$source_dir\Platforms\Exceptionless.NLog";		        ExternalNuGetDependencies = "NLog";		MergeDependencies = $null; }
    @{ Name = "Exceptionless.NLog.Signed"; 		SourceDir = "$source_dir\Platforms\Exceptionless.NLog.Signed";		ExternalNuGetDependencies = "NLog";		MergeDependencies = $null; }
    @{ Name = "Exceptionless.Log4net"; 			SourceDir = "$source_dir\Platforms\Exceptionless.Log4net";	        ExternalNuGetDependencies = "log4net";	MergeDependencies = $null; }
    @{ Name = "Exceptionless.Log4net.Signed"; 	SourceDir = "$source_dir\Platforms\Exceptionless.Log4net.Signed";	ExternalNuGetDependencies = "log4net";	MergeDependencies = $null; }
)

$client_build_configurations = @(
    @{ Constants = "NET40"; TargetFrameworkVersionProperty="NET40";	NuGetDir = "net40"; },
    @{ Constants = "NET45"; TargetFrameworkVersionProperty="NET45";	NuGetDir = "net45"; }
)