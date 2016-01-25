using System;
using Nancy;

namespace Exceptionless.SampleNancy {
    public class ExceptionlessModule : NancyModule {
        public ExceptionlessModule() {
            Get["/"] = _ => "Hello!";
            Get["/boom"] = _ => { throw new Exception("Unhandled Exception"); };
            Get["/custom"] = _ => {
                new Exception("Handled Exception").ToExceptionless().AddRequestInfo(Context).Submit();
                return "ok, handled";
            };
        }
    }
}
