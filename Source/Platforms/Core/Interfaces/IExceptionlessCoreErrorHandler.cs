using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExceptionLess.Core.Interfaces
{
    /// <summary>
    /// IExceptionlessCoreHandlerError.
    /// </summary>   
    ///     
    public interface IExceptionlessCoreErrorHandler
    {
        /// <summary>
        /// Handles AspNetCore exception asynchronously.
        /// </summary>
        /// <param name="exceptionlessCoreErrorAttribute">The exception context.</param>
        /// <returns></returns>
        Task HandleAsync(IExceptionlessCoreErrorAttribute exceptionlessCoreErrorAttribute);
    }  

}
