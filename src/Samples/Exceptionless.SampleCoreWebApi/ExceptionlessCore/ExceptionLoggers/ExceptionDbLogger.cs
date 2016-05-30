using Exceptionless.SampleCoreWebApi;
using ExceptionLess.AspNetCore.Interfaces;
using System.Threading.Tasks;

namespace Exceptionless.Exceptionless.SampleCoreWebApi
    {
    public class ExceptionDbLogger : IExceptionlessCoreErrorHandler
    {
        public Task HandleErrorAsync(IExceptionlessCoreErrorAttribute exceptionContext)
        {
            var category = (ExceptionCategory)exceptionContext.Context.Items["exception.category"];
            if (category.Category == ExceptionCategoryType.Unhandled)
            {
                dynamic response = exceptionContext.Context.Items["exception.response"];

                // log whatever to the Database
                // Note: Application Insights may be a more attractive analytical logger than rolling your own.
            }

            return Task.FromResult(0);
        }
    }
}
