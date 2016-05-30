using ExceptionLess.AspNetCore.Interfaces;
using System;
using System.Dynamic;
using System.Threading.Tasks;

namespace Exceptionless.SampleCoreMvc
    {
    public class ExceptionInitializer : IExceptionlessCoreErrorHandler
    {
        private readonly ExceptionCategorizer _exceptionCategorizer;

        public ExceptionInitializer(ExceptionCategorizer exceptionCategorizer)
        {
            _exceptionCategorizer = exceptionCategorizer;
        }

        public Task HandleErrorAsync(IExceptionlessCoreErrorAttribute exceptionContext)
        {
            var category = _exceptionCategorizer.Categorizer(exceptionContext.Exception);
            dynamic response = new ExpandoObject();

            response.Status = category.HttpStatus;
            response.TrackingId = Guid.NewGuid().ToString();
            response.Timestamp = DateTimeOffset.Now.ToString();
            response.Message = category.ErrorMessage;
            response.Execution = "Global";

            if (exceptionContext.Context.Request != null)
            {
                response.Execution = "Request";

                if (category.Category == ExceptionCategoryType.Unhandled)
                {
                    response.Developer = new ExpandoObject();
                    response.Developer.RequestMethod = exceptionContext.Context.Request.Method;
                    response.Developer.Uri = $"{exceptionContext.Context.Request.Scheme}:{exceptionContext.Context.Request.Host}{exceptionContext.Context.Request.Path}";
                    response.Developer.ExceptionType = exceptionContext.Exception.GetType().FullName;
                    response.Developer.StackTrace = exceptionContext.Exception.StackTrace.Trim();
                }
            }

            exceptionContext.Context.Items["exception.category"] = category;
            exceptionContext.Context.Items["exception.response"] = response;

            return Task.FromResult(0);
        }
    }
}
