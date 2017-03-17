$configuration = "Release"
$base_dir = Resolve-Path "..\"
$artifacts_dir = "$base_dir\artifacts"
$build_dir = "$base_dir\build"
$source_dir = "$base_dir\src"
$working_dir = "$build_dir\working"
$sign_file = "$source_dir\Exceptionless.snk"

$client_projects = @(
    @{ Name = "Exceptionless"; 					    SourceDir = "$source_dir\Exceptionless"; 	                    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Signed"; 			    SourceDir = "$source_dir\Exceptionless"; 	                    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Portable"; 		    SourceDir = "$source_dir\Exceptionless.Portable";			    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Portable.Signed"; 	    SourceDir = "$source_dir\Exceptionless.Portable";			    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.AspNetCore";  		    SourceDir = "$source_dir\Platforms\Exceptionless.AspNetCore";   ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.AspNetCore.Signed";    SourceDir = "$source_dir\Platforms\Exceptionless.AspNetCore";   ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Mvc";  			    SourceDir = "$source_dir\Platforms\Exceptionless.Mvc"; 		    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Mvc.Signed";  		    SourceDir = "$source_dir\Platforms\Exceptionless.Mvc"; 		    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Nancy";  			    SourceDir = "$source_dir\Platforms\Exceptionless.Nancy"; 	    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.WebApi";  			    SourceDir = "$source_dir\Platforms\Exceptionless.WebApi"; 	    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.WebApi.Signed";    	SourceDir = "$source_dir\Platforms\Exceptionless.WebApi"; 	    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Web"; 				    SourceDir = "$source_dir\Platforms\Exceptionless.Web"; 		    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Web.Signed"; 		    SourceDir = "$source_dir\Platforms\Exceptionless.Web"; 		    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Windows"; 			    SourceDir = "$source_dir\Platforms\Exceptionless.Windows"; 	    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Windows.Signed"; 	    SourceDir = "$source_dir\Platforms\Exceptionless.Windows"; 	    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Wpf"; 				    SourceDir = "$source_dir\Platforms\Exceptionless.Wpf"; 		    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.Wpf.Signed"; 		    SourceDir = "$source_dir\Platforms\Exceptionless.Wpf"; 		    ExternalNuGetDependencies = $null;      UseMSBuild = $false; },
    @{ Name = "Exceptionless.NLog"; 			    SourceDir = "$source_dir\Platforms\Exceptionless.NLog";		    ExternalNuGetDependencies = "NLog";     UseMSBuild = $false; }
    @{ Name = "Exceptionless.NLog.Signed"; 		    SourceDir = "$source_dir\Platforms\Exceptionless.NLog";		    ExternalNuGetDependencies = "NLog";     UseMSBuild = $false; }
    @{ Name = "Exceptionless.Log4net"; 			    SourceDir = "$source_dir\Platforms\Exceptionless.Log4net";	    ExternalNuGetDependencies = "log4net";  UseMSBuild = $false; }
    @{ Name = "Exceptionless.Log4net.Signed";   	SourceDir = "$source_dir\Platforms\Exceptionless.Log4net";	    ExternalNuGetDependencies = "log4net";  UseMSBuild = $false; }

    @{ Name = "Exceptionless.Windows"; 			    SourceDir = "$source_dir\Platforms\Exceptionless.Windows"; 	    ExternalNuGetDependencies = $null;      UseMSBuild = $true; },
    @{ Name = "Exceptionless.Windows.Signed"; 	    SourceDir = "$source_dir\Platforms\Exceptionless.Windows"; 	    ExternalNuGetDependencies = $null;      UseMSBuild = $true; },
    @{ Name = "Exceptionless.Wpf"; 				    SourceDir = "$source_dir\Platforms\Exceptionless.Wpf"; 		    ExternalNuGetDependencies = $null;      UseMSBuild = $true; },
    @{ Name = "Exceptionless.Wpf.Signed"; 		    SourceDir = "$source_dir\Platforms\Exceptionless.Wpf"; 		    ExternalNuGetDependencies = $null;      UseMSBuild = $true; },
)

$client_build_configurations = @(
    @{ Constants = "NET45"; TargetFrameworkVersionProperty="NET45";	NuGetDir = "net45"; }
)