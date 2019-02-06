using System;
using System.Collections.Generic;
using Exceptionless.Logging;
using Microsoft.AspNetCore.Mvc;

namespace Exceptionless.SampleAspNetCore.Controllers {
    [Route("api/[controller]")]
    public class ValuesController : Controller {
        // GET api/values
        [HttpGet]
        public Dictionary<string, string> Get() {
            ExceptionlessClient.Default.CreateLog("ValuesController", "Getting results", LogLevel.Info).Submit();
            throw new Exception($"Random AspNetCore Exception: {Guid.NewGuid()}");
        }
    }
}