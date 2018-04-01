using System;
using System.Reflection;

[assembly: AssemblyProduct("Exceptionless")]
[assembly: AssemblyCompany("Exceptionless")]
[assembly: AssemblyTrademark("Exceptionless")]
[assembly: AssemblyCopyright("Copyright (c) 2017 Exceptionless.  All rights reserved.")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyVersion("4.0.0")]
[assembly: AssemblyFileVersion("4.0.0")]
[assembly: AssemblyInformationalVersion("4.0.0")]

internal sealed partial class ThisAssembly {
    internal const string AssemblyCompany = "Exceptionless";

    internal const string AssemblyProduct = "Exceptionless";

    internal const string AssemblyTrademark = "Exceptionless";

    internal const string AssemblyCopyright = "Copyright (c) 2018 Exceptionless.  All rights reserved.";

    internal const string AssemblyConfiguration = "Release";

    internal const string AssemblyVersion = "4.0.0";

    internal const string AssemblyFileVersion = "4.0.0";

    internal const string AssemblyInformationalVersion = "4.0.0";

    private ThisAssembly() {}
}
