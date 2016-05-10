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
    public interface IExceptionlessCoreHandlerError
    {
        /// <summary>
        /// Handles exception asynchronously.
        /// </summary>
        /// <param name="exceptionContext">The exception context.</param>
        /// <returns></returns>
        Task HandleAsync(IExceptionlessCoreErrorAttribute exceptionContext);
    }  

}
