using Exceptionless.AspNetCore.Interfaces;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System;

namespace Exceptionless.SampleCoreMvc.ExceptionlessCore
    {
    public class ExceptionFinalizer: IExceptionlessCoreErrorHandler
        {
        /// <summary>
        /// Handles exception asynchronously.
        /// </summary>
        /// <param name="exceptionContext">The exception context.</param>
        /// <returns></returns>
        public async Task HandleErrorAsync(IExceptionlessCoreErrorAttribute exceptionContext)
            {
            var category = (ExceptionCategory)exceptionContext.Context.Items["exception.category"];
            dynamic response = exceptionContext.Context.Items["exception.response"];
            dynamic finalResponse = category.DeveloperMode ? response : response.System;

            exceptionContext.Context.Response.StatusCode = (int)category.HttpStatus;
            exceptionContext.Context.Response.ContentType = "application/json";
            await exceptionContext.Context.Response.WriteAsync((string)JsonConvert.SerializeObject(finalResponse, Formatting.Indented));
            }        
        }
    }
