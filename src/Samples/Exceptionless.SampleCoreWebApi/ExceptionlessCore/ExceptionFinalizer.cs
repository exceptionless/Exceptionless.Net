using Exceptionless.AspNetCore.Interfaces;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Exceptionless.SampleCoreWebApi.ExceptionlessCore
    {
    public class ExceptionFinalizer : IExceptionlessCoreErrorHandler
        {
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
