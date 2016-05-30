using ExceptionLess.AspNetCore.Interfaces;
using System.Threading.Tasks;

namespace Exceptionless.SampleCoreMvc
    {
    public class ExceptionJIRALogger : IExceptionlessCoreErrorHandler
    {
        public Task HandleErrorAsync(IExceptionlessCoreErrorAttribute exceptionContext)
        {
            var category = (ExceptionCategory)exceptionContext.Context.Items["exception.category"];
            if (category.Category == ExceptionCategoryType.Unhandled)
            {
                dynamic response = exceptionContext.Context.Items["exception.response"];

                // log whatever to the JIRA for production issue tracking
            }

            return Task.FromResult(0);
        }
    }
}
