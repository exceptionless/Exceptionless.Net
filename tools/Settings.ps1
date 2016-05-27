$configuration = "Release"
$base_dir = Resolve-Path "..\"
$artifacts_dir = "$base_dir\artifacts"
$build_dir = "$base_dir\build"
$source_dir = "$base_dir\src"
$working_dir = "$build_dir\working"
$sign_file = "$source_dir\Exceptionless.snk"

$client_projects = @(
    @{ Name = "Exceptionless.Portable"; 		SourceDir = "$source_dir\Exceptionless.Portable";			        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Portable.Signed"; 	SourceDir = "$source_dir\Exceptionless.Portable.Signed";			ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Extras"; 		    SourceDir = "$source_dir\Exceptionless.Extras";			            ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Extras.Signed"; 	SourceDir = "$source_dir\Exceptionless.Extras.Signed";			    ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Mvc";  			SourceDir = "$source_dir\Platforms\Exceptionless.Mvc"; 		        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Mvc.Signed";  		SourceDir = "$source_dir\Platforms\Exceptionless.Mvc.Signed"; 		ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Nancy";  			SourceDir = "$source_dir\Platforms\Exceptionless.Nancy"; 	        ExternalNuGetDependencies = $null; },
	@{ Name = "Exceptionless.Nancy.Signet";  			SourceDir = "$source_dir\Platforms\Exceptionless.Nancy.Signet"; 	        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.WebApi";  			SourceDir = "$source_dir\Platforms\Exceptionless.WebApi"; 	        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.WebApi.Signed";  	SourceDir = "$source_dir\Platforms\Exceptionless.WebApi.Signed"; 	ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Web"; 				SourceDir = "$source_dir\Platforms\Exceptionless.Web"; 		        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Web.Signed"; 		SourceDir = "$source_dir\Platforms\Exceptionless.Web.Signed"; 		ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Windows"; 			SourceDir = "$source_dir\Platforms\Exceptionless.Windows"; 	        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Windows.Signed"; 	SourceDir = "$source_dir\Platforms\Exceptionless.Windows"; 	        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless"; 					SourceDir = "$source_dir\Platforms\Exceptionless"; 	                ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Signed"; 			SourceDir = "$source_dir\Platforms\Exceptionless.Signed"; 	        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Wpf"; 				SourceDir = "$source_dir\Platforms\Exceptionless.Wpf"; 		        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.Wpf.Signed"; 		SourceDir = "$source_dir\Platforms\Exceptionless.Wpf"; 		        ExternalNuGetDependencies = $null; },
    @{ Name = "Exceptionless.NLog"; 			SourceDir = "$source_dir\Platforms\Exceptionless.NLog";		        ExternalNuGetDependencies = "NLog"; }
    @{ Name = "Exceptionless.NLog.Signed"; 		SourceDir = "$source_dir\Platforms\Exceptionless.NLog.Signed";		ExternalNuGetDependencies = "NLog"; }
    @{ Name = "Exceptionless.Log4net"; 			SourceDir = "$source_dir\Platforms\Exceptionless.Log4net";	        ExternalNuGetDependencies = "log4net"; }
    @{ Name = "Exceptionless.Log4net.Signed"; 	SourceDir = "$source_dir\Platforms\Exceptionless.Log4net.Signed";	ExternalNuGetDependencies = "log4net"; }
	@{ Name = "Exceptionless.NetCore"; 	SourceDir = "$source_dir\Platforms\Exceptionless.NetCore";	ExternalNuGetDependencies = "NetCore"; }
	@{ Name = "Exceptionless.NetCore.Signed"; 	SourceDir = "$source_dir\Platforms\Exceptionless.NetCore.Signed";	ExternalNuGetDependencies = "NetCore"; }
	
)

$client_build_configurations = @(
    @{ Constants = "NET40"; TargetFrameworkVersionProperty="NET40";	NuGetDir = "net40"; },
    @{ Constants = "NET45"; TargetFrameworkVersionProperty="NET45";	NuGetDir = "net45"; },
	@{ Constants = "NETSTANDARD15"; TargetFrameworkVersionProperty="NETSTANDARD15";	NuGetDir = "netstandard15"; }
)