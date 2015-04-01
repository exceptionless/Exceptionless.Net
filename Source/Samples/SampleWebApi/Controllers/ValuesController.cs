using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Exceptionless.SampleWebApi.Controllers {
    public class ValuesController : ApiController {
        // GET api/values
        public IEnumerable<string> Get() {
            throw new ApplicationException("WebApi GET error");
        }

        // GET api/values/5
        public string Get(int id) {
            return "value";
        }

        // POST api/values
        public void Post([FromBody] string value) {
            throw new ApplicationException("WebApi POST error");
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value) {}

        // DELETE api/values/5
        public void Delete(int id) {}
    }
}