using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Exceptionless.Logging;
using Exceptionless.Models.Data;

namespace Exceptionless.Services {
    public class DefaultEnvironmentInfoCollector : IEnvironmentInfoCollector {
        private static EnvironmentInfo _environmentInfo;
        private readonly ExceptionlessConfiguration _config;
        private readonly IExceptionlessLog _log;

        public DefaultEnvironmentInfoCollector(ExceptionlessConfiguration config, IExceptionlessLog log) {
            _config = config;
            _log = log;
        }

        public EnvironmentInfo GetEnvironmentInfo() {
            if (_environmentInfo != null) {
                PopulateThreadInfo(_environmentInfo);
                PopulateMemoryInfo(_environmentInfo);
                return _environmentInfo;
            }

            var info = new EnvironmentInfo();
            PopulateRuntimeInfo(info);
            PopulateProcessInfo(info);
            PopulateThreadInfo(info);
            PopulateMemoryInfo(info);

            _environmentInfo = info;
            return _environmentInfo;
        }

        private void PopulateApplicationInfo(EnvironmentInfo info) {
            try {
                info.Data.Add("AppDomainName", AppDomain.CurrentDomain.FriendlyName);
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get AppDomain friendly name. Error message: {0}", ex.Message);
            }

            if (_config.IncludeIpAddress) {
                try {
                    IPHostEntry hostEntry = Dns.GetHostEntryAsync(Dns.GetHostName()).ConfigureAwait(false).GetAwaiter().GetResult();
                    if (hostEntry != null && hostEntry.AddressList.Any())
                        info.IpAddress = String.Join(", ", hostEntry.AddressList.Where(x => x.AddressFamily == AddressFamily.InterNetwork).Select(a => a.ToString()).ToArray());
                } catch (Exception ex) {
                    _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get ip address. Error message: {0}", ex.Message);
                }
            }
        }

        private void PopulateProcessInfo(EnvironmentInfo info) {
            try {
                info.ProcessorCount = Environment.ProcessorCount;
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get processor count. Error message: {0}", ex.Message);
            }

            try {
                Process process = Process.GetCurrentProcess();
                info.ProcessName = process.ProcessName;
                info.ProcessId = process.Id.ToString(NumberFormatInfo.InvariantInfo);
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get process name or id. Error message: {0}", ex.Message);
            }

            try {
                info.CommandLine = Environment.CommandLine;
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get command line. Error message: {0}", ex.Message);
            }
        }

        private void PopulateThreadInfo(EnvironmentInfo info) {
            try {
                info.ThreadId = Thread.CurrentThread.ManagedThreadId.ToString(NumberFormatInfo.InvariantInfo);
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get thread id. Error message: {0}", ex.Message);
            }

            try {
                info.ThreadName = Thread.CurrentThread.Name;
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get current thread name. Error message: {0}", ex.Message);
            }
        }

        private void PopulateMemoryInfo(EnvironmentInfo info) {
            try {
                Process process = Process.GetCurrentProcess();
                info.ProcessMemorySize = process.PrivateMemorySize64;
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get process memory size. Error message: {0}", ex.Message);
            }

#if NET45
            try {
                if (IsMonoRuntime) {
                    if (PerformanceCounterCategory.Exists("Mono Memory")) {
                        var totalPhysicalMemory = new PerformanceCounter("Mono Memory", "Total Physical Memory");
                        info.TotalPhysicalMemory = Convert.ToInt64(totalPhysicalMemory.RawValue);

                        var availablePhysicalMemory = new PerformanceCounter("Mono Memory", "Available Physical Memory"); //mono 4.0+
                        info.AvailablePhysicalMemory = Convert.ToInt64(availablePhysicalMemory.RawValue);
                    }
                } else {
                    var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                    info.TotalPhysicalMemory = Convert.ToInt64(computerInfo.TotalPhysicalMemory);
                    info.AvailablePhysicalMemory = Convert.ToInt64(computerInfo.AvailablePhysicalMemory);
                }
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get physical memory. Error message: {0}", ex.Message);
            }
#endif
        }

#if NET45
        private bool IsMonoRuntime {
            get {
                try {
                    return Type.GetType("Mono.Runtime") != null;
                } catch (Exception) {
                    return false;
                }
            }
        }
#endif

        private void PopulateRuntimeInfo(EnvironmentInfo info) {
#if NETSTANDARD
            info.OSName = GetOSName(RuntimeInformation.OSDescription);
            info.OSVersion = GetVersion(RuntimeInformation.OSDescription)?.ToString();
            info.Architecture = RuntimeInformation.OSArchitecture.ToString();
            info.Data["FrameworkDescription"] = RuntimeInformation.FrameworkDescription;
            info.Data["ProcessArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString();
#endif

            if (_config.IncludeMachineName) {
                try {
                    info.MachineName = Environment.MachineName;
                } catch (Exception ex) {
                    _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get machine name. Error message: {0}", ex.Message);
                }
            }

#if NETSTANDARD
            Microsoft.Extensions.PlatformAbstractions.PlatformServices computerInfo = null;
#elif NET45
            Microsoft.VisualBasic.Devices.ComputerInfo computerInfo = null;
#endif

            try {
#if NETSTANDARD
                computerInfo = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default;
#elif NET45
                computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
#endif
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get computer info. Error message: {0}", ex.Message);
            }

            if (computerInfo == null)
                return;

            try {
#if NETSTANDARD
                info.RuntimeVersion = computerInfo.Application.RuntimeFramework.Version.ToString();
                info.Data["ApplicationBasePath"] = computerInfo.Application.ApplicationBasePath;
                info.Data["ApplicationName"] = computerInfo.Application.ApplicationName;
                info.Data["RuntimeFramework"] = computerInfo.Application.RuntimeFramework.FullName;
#elif NET45
                info.OSName = computerInfo.OSFullName;
                info.OSVersion = computerInfo.OSVersion;
                info.RuntimeVersion = Environment.Version.ToString();
                info.Architecture = Is64BitOperatingSystem() ? "x64" : "x86";
#endif
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get populate runtime info. Error message: {0}", ex.Message);
            }
        }

#if NETSTANDARD
        private string GetOSName(string osDescription) {
            if (String.IsNullOrEmpty(osDescription))
                return null;

            var version = GetVersion(osDescription);
            if (version != null)
                return osDescription.Replace(version.ToString(), version.ToString(version.Minor == 0 ? 1 : 2));

            return osDescription;
        }

        private Version GetVersion(string description) {
            if (String.IsNullOrEmpty(description))
                return null;

            var parts = description.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0) {
                Version version;
                if (Version.TryParse(parts[parts.Length - 1], out version))
                    return version;
            }

            return null;
        }
#endif

#if NET45
        private bool Is64BitOperatingSystem() {
            if (IntPtr.Size == 8) // 64-bit programs run only on Win64
                return true;

            try {
                // Detect whether the current process is a 32-bit process running on a 64-bit system.
                bool is64;
                bool methodExist = KernelNativeMethods.MethodExists("kernel32.dll", "IsWow64Process");

                return ((methodExist && KernelNativeMethods.IsWow64Process(KernelNativeMethods.GetCurrentProcess(), out is64)) && is64);
            } catch (Exception ex) {
                _log.FormattedWarn(typeof(DefaultEnvironmentInfoCollector), "Unable to get CPU architecture. Error message: {0}", ex.Message);
            }

            return false;
        }

        private static class KernelNativeMethods {
        #region Kernel32

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string procName);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

            [DllImport("kernel32.dll")]
            public static extern IntPtr GetCurrentProcess();

            [DllImport("kernel32.dll")]
            public static extern int GetCurrentProcessId();

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [PreserveSig]
            public static extern int GetModuleFileName([In] IntPtr hModule, [Out] StringBuilder lpFilename, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr GetModuleHandle(string moduleName);

            [DllImport("kernel32.dll")]
            public static extern int GetCurrentThreadId();

        #endregion

            public static bool MethodExists(string moduleName, string methodName) {
                IntPtr moduleHandle = GetModuleHandle(moduleName);
                if (moduleHandle == IntPtr.Zero)
                    return false;

                return (GetProcAddress(moduleHandle, methodName) != IntPtr.Zero);
            }
        }
#endif
    }
}
