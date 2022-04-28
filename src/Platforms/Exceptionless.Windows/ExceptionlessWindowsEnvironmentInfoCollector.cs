using System;
using System.Diagnostics;
using Exceptionless.Logging;
using Exceptionless.Models.Data;
using Exceptionless.Services;

namespace Exceptionless {
    public class ExceptionlessWindowsEnvironmentInfoCollector : DefaultEnvironmentInfoCollector {
        public ExceptionlessWindowsEnvironmentInfoCollector(ExceptionlessConfiguration config, IExceptionlessLog log) : base(config, log) { }

        public override EnvironmentInfo GetEnvironmentInfo() {
            EnvironmentInfo info = base.GetEnvironmentInfo();
            PopulateHandleInfo(info);
            return info;
        }

        private void PopulateHandleInfo(EnvironmentInfo info) {
            try {
                using (Process currentProcess = Process.GetCurrentProcess()) {
                    info.Data["Handles"] = currentProcess.HandleCount;
                    info.Data["UserObjects"] = User32NativeMethods.GetGuiResources(currentProcess.Handle, (int)User32NativeMethods.UIFlags.UserObjectCount);
                    info.Data["GDIObjects"] = User32NativeMethods.GetGuiResources(currentProcess.Handle, (int)User32NativeMethods.UIFlags.GDIObjectCount);
                }
            }
            catch (Exception ex) {
                Log.FormattedWarn(typeof(ExceptionlessWindowsEnvironmentInfoCollector), "Unable to get process handles, User objects, or GDI objects. Error message: {0}", ex.Message);
            }
        }

        private static class User32NativeMethods {
            #region User32

            /// <summary>
            /// <para>
            /// Retrieves the count of handles to graphical user interface
            /// (GUI) objects in use by the specified process.
            /// </para>
            /// API reference: <see href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguiresources"/>
            /// </summary>
            /// <param name="hProcess"></param>
            /// <param name="uiFlags"></param>
            /// <returns></returns>
            [System.Runtime.InteropServices.DllImport("User32.dll")]
            public static extern int GetGuiResources(IntPtr hProcess, uint uiFlags);

            #endregion

            /// <summary>
            /// Enum representing the possible values to pass to <see cref="GetGuiResources(IntPtr, UInt32)"/>.
            /// </summary>
            public enum UIFlags {
                /// <summary>
                /// Return the count of GDI objects.
                /// </summary>
                GDIObjectCount = 0,
                /// <summary>
                /// Return the count of USER objects.
                /// </summary>
                UserObjectCount = 1,
            }

        }
    }
}
