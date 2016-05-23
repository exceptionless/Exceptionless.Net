using Microsoft.AspNetCore.Http;
using System;


namespace ExceptionLess.Core.Interfaces
{
    public interface IExceptionlessCoreErrorAttribute
    {
        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        HttpContext Context { get; }
        
        /// <summary>
        /// Gets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        Exception Exception { get; }

        
        
    }
}

