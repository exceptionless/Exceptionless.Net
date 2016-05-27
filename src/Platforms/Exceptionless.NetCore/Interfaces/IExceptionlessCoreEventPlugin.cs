using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExceptionLess.NetCore.Interfaces
{
    /// <summary>
    /// IExceptionlessCoreEventPlugin.cs.
    /// </summary>   
    ///     
    public interface IExceptionlessCoreEventPlugin
        {
        /// <summary>
        /// Handles AspNetCore exception asynchronously.
        /// </summary>
        /// <param name="exceptionlessCoreErrorAttribute">The exception context.</param>
        /// <returns></returns>
        Task EventPlugin(IExceptionlessCoreErrorAttribute exceptionlessCoreErrorAttribute);
    }  

}
