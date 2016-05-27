using ExceptionLess.NetCore.Interfaces;
using Microsoft.AspNetCore.Http;
using System;


namespace ExceptionLess.NetCore
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
