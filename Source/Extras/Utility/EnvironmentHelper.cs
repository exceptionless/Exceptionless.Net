using System;

namespace Exceptionless.Extras.Utility
{
    public static class EnvironmentHelper
    {
        /// <summary>
        /// Determine current os platform.
        /// </summary>
        /// <exception cref="InvalidOperationException" accessor="get"></exception>
        public static bool IsUnix
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
    }
}
