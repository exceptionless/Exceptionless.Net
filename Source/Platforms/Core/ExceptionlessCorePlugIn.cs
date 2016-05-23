using ExceptionLess.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExceptionLess.Core
{
    public class ExceptionlessCorePlugIn
    {
        private readonly IList<IExceptionlessCoreErrorHandler> _exceptionlessCoreHandlerError;

        /// <summary>
        /// </summary>
        public ExceptionlessCorePlugIn()
        {
            _exceptionlessCoreHandlerError = new List<IExceptionlessCoreErrorHandler>();
        }

        /// <summary>
        /// Intercepts the specified context.
        /// </summary>
        /// <param name="exceptionlessCoreErrorAttribute">The context.</param>
        /// <returns></returns>
        /// <exception cref="AggregateException"></exception>
        /// <exception cref="System.AggregateException"></exception>
        public async Task CoreAsync(IExceptionlessCoreErrorAttribute exceptionlessCoreErrorAttribute)
        {
            var handlerExecutionExceptions = new List<Exception>();
            try
            {
                foreach (var handler in _exceptionlessCoreHandlerError)
                {
                    try
                    {
                        await handler.HandleAsync(exceptionlessCoreErrorAttribute);
                    }
                    catch (Exception ex)
                    {
                        handlerExecutionExceptions.Add(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                handlerExecutionExceptions.Add(ex);
            }
            finally
            {
                if (handlerExecutionExceptions.Any())
                {
                    throw new AggregateException(handlerExecutionExceptions);
                }
            }
        }

        /// <summary>
        /// Adds and exception filter.
        /// </summary>
        /// <param name="exceptionlessCoreHandlerError">The exception Handler Error.</param>
        public void AddExceptionlessCoreHandlerError(IExceptionlessCoreErrorHandler exceptionlessCoreHandlerError)
        {
            _exceptionlessCoreHandlerError.Add(exceptionlessCoreHandlerError);
        }
    }
}
