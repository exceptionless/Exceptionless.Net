using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Exceptionless.SampleAspNetCore.Controllers {
    [Route("api/[controller]")]
    public class ValuesController : Controller {
        // GET api/values
        [HttpGet]
        public Dictionary<string, string> Get() {
            throw new Exception($"Random AspNetCore Exception: {Guid.NewGuid()}");
        }
    }
}