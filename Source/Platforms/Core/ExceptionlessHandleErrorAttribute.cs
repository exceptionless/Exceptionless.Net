using ExceptionLess.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System;


namespace ExceptionLess.Core
{
    public class ExceptionlessHandleErrorAttribute : IExceptionlessCoreErrorAttribute
    {
        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        public HttpContext Context
        {
            get; set;
        }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        public Exception Exception
        {
            get; set;
        }
    }
}
