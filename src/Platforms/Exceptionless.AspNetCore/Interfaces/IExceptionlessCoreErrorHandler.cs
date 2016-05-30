using System.Threading.Tasks;

namespace ExceptionLess.AspNetCore.Interfaces
    {
    /// <summary>
    /// IExceptionlessCoreErrorHandler.cs.
    /// </summary>   
    ///     
    public interface IExceptionlessCoreErrorHandler
        {
        /// <summary>
        /// Handles AspNetCore exception asynchronously.
        /// </summary>
        /// <param name="exceptionlessCoreErrorAttribute">The exception context.</param>
        /// <returns></returns>
        Task HandleErrorAsync(IExceptionlessCoreErrorAttribute exceptionlessCoreErrorAttribute);
    }  

}
