using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Exceptionless.SampleMvc.Controllers {
    public class SomeModel {
        public string Test { get; set; }
        public string Blah { get; set; }
    }

    [HandleError(View = "CustomError", ExceptionType = typeof(ArgumentException))]
    public class HomeController : Controller {
        public ActionResult Index() {
            return View();
        }

        [HttpPost]
        public ViewResult Index(SomeModel model) {
            throw new MyApplicationException("Error on form submit.") {
                IgnoredProperty = "Index Test",
                RandomValue = Guid.NewGuid().ToString()
            };
        }

        [HttpGet]
        public ViewResult Error() {
            return View("Error");
        }

        [HttpGet]
        public ViewResult CustomError() {
            return View("CustomError");
        }
        
        [HttpGet]
        public ViewResult ManualStacking(string myId) {
            ExceptionlessClient.Default.CreateLog(nameof(HomeController), "Random Log message")
                .SetManualStackingInfo("Manual Stacked Log Messages", new Dictionary<string, string> {
                    { "Controller", nameof(HomeController) },
                    { "Action", nameof(ManualStacking) }
                })
                .Submit();

            try {
                throw new Exception(Guid.NewGuid().ToString());
            } catch (Exception ex) {
                ex.ToExceptionless().SetManualStackingKey(nameof(HomeController)).Submit();
                throw;
            }
        }

        [HttpGet]
        public ViewResult FourZeroFour() {
            throw new HttpException(404, "custom 404");
        }

        [HttpPost]
        public JsonResult AjaxMethod(SomeModel model) {
            throw new ApplicationException("Error on AJAX call.");
        }

        [HttpPost]
        public async Task<ActionResult> Error(string identifier, string emailAddress, string description) {
            if (String.IsNullOrEmpty(identifier))
                return RedirectToAction("Index", "Home");

            if (String.IsNullOrEmpty(emailAddress) && String.IsNullOrEmpty(description))
                return RedirectToAction("Index", "Home");

            await ExceptionlessClient.Default.UpdateUserEmailAndDescriptionAsync(identifier, emailAddress, description);

            return View("ErrorSubmitted");
        }

        public ActionResult NotFound(string url = null) {
            ViewBag.Url = url;
            return View();
        }

        [HttpGet]
        public ActionResult Boom() {
            throw new ApplicationException("Boom!");
        }

        [HttpGet]
        public ActionResult CustomBoom() {
            throw new ArgumentException("Boom!");
        }

        [HttpGet]
        public ActionResult Boom25() {
            for (int i = 0; i < 25; i++) {
                try {
                    throw new MyApplicationException("Boom!") {
                        IgnoredProperty = "Test",
                        RandomValue = Guid.NewGuid().ToString()
                    };
                } catch (Exception ex) {
                    ex.ToExceptionless()
                        .SetUserIdentity("some@email.com")
                        .AddRecentTraceLogEntries()
                        .AddRequestInfo()
                        .AddObject(new { Blah = "Hello" }, name: "Hello")
                        .AddTags("SomeTag", "AnotherTag")
                        .MarkAsCritical()
                        .Submit();

                    ex.ToExceptionless().Submit();

                    ex.ToExceptionless()
                        .SetUserIdentity("some@email.com")
                        .SetUserDescription("some@email.com", "Some description.")
                        .AddRecentTraceLogEntries()
                        .AddRequestInfo()
                        .AddObject(new { Blah = "Hello" }, name: "Hello", excludedPropertyNames: new[] { "Blah" })
                        .AddTags("SomeTag", "AnotherTag")
                        .MarkAsCritical()
                        .Submit();
                }

                Thread.Sleep(1500);
            }

            return RedirectToAction("Index");
        }

        public ActionResult CreateRequestValidationException(string value) {
            return RedirectToAction("Index");
        }
    }

    public class MyApplicationException : ApplicationException {
        public MyApplicationException(string message) : base(message) {}

        public string IgnoredProperty { get; set; }

        public string RandomValue { get; set; }   
    }
}