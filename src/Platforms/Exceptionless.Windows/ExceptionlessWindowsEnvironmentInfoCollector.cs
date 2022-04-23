using System;
using System.Diagnostics;
using Exceptionless.Logging;
using Exceptionless.Models.Data;
using Exceptionless.Services;

namespace Exceptionless {
    public class ExceptionlessWindowsEnvironmentInfoCollector : DefaultEnvironmentInfoCollector {
        private readonly IExceptionlessLog _log;
        public ExceptionlessWindowsEnvironmentInfoCollector(ExceptionlessConfiguration config, IExceptionlessLog log) : base(config, log) {
            _log = log;
        }

        public new EnvironmentInfo GetEnvironmentInfo() {
            EnvironmentInfo info = base.GetEnvironmentInfo();
            PopulateHandleInfo(info);
            return info;
        }

        private void PopulateHandleInfo(EnvironmentInfo info) {
            try {
                Process process = Process.GetCurrentProcess();
                info.Handles = process.HandleCount;
                info.UserObjects = User32NativeMethods.GetGuiResources(process.Handle, (int)User32NativeMethods.UIFlags.UserObjectCount);
                info.GDIObjects = User32NativeMethods.GetGuiResources(process.Handle, (int)User32NativeMethods.UIFlags.GDIObjectCount);
            }
            catch (Exception ex) {
                _log.FormattedWarn(typeof(ExceptionlessWindowsEnvironmentInfoCollector), "Unable to get process handles, User objects, or GDI objects. Error message: {0}", ex.Message);
            }
        }

        private static class User32NativeMethods {
            #region User32

            [System.Runtime.InteropServices.DllImport("User32.dll")]
            public static extern int GetGuiResources(IntPtr hProcess, int uiFlags);

            #endregion

            /// <summary>
            /// Enum representing the possible values to pass to <see cref="GetGuiResources(IntPtr, int)"/>.
            /// </summary>
            public enum UIFlags {
                GDIObjectCount = 0,
                UserObjectCount = 1,
            }

        }
    }
}
