using System;
using System.Reflection;

[assembly: AssemblyProduct("Exceptionless")]
[assembly: AssemblyCompany("Exceptionless")]
[assembly: AssemblyTrademark("Exceptionless")]
[assembly: AssemblyCopyright("Copyright (c) 2015 Exceptionless.  All rights reserved.")]
#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyVersion("2.0.0")]
[assembly: AssemblyFileVersion("2.0.0")]
[assembly: AssemblyInformationalVersion("2.0.0")]

internal sealed partial class ThisAssembly {
    internal const string AssemblyCompany = "Exceptionless";

    internal const string AssemblyProduct = "Exceptionless";

    internal const string AssemblyTrademark = "Exceptionless";

    internal const string AssemblyCopyright = "Copyright (c) 2015 Exceptionless.  All rights reserved.";

    internal const string AssemblyConfiguration = "Release";

    internal const string AssemblyVersion = "2.0.0";

    internal const string AssemblyFileVersion = "2.0.0";

    internal const string AssemblyInformationalVersion = "2.0.0";

    private ThisAssembly() {}
}
