using System;
using System.Collections.Generic;
using System.Web.Http;

namespace Exceptionless.SampleMvc.Controllers {
    public class ValuesController : ApiController {
        // GET api/values
        public IEnumerable<string> Get() {
            try {
                throw new ApplicationException("WebApi GET error");
            } catch (Exception ex) {
                ex.ToExceptionless().Submit();
                throw;
            }
        }

        // GET api/values/5
        public string Get(int id) {
            return "value";
        }

        // POST api/values
        public void Post([FromBody] Person person) {
            throw new ApplicationException("WebApi POST error");
        }

        // PUT api/values/5
        public void Put(int id, [FromBody] string value) {}

        // DELETE api/values/5
        public void Delete(int id) {}
    }

    public class Person {
        public string Name { get; set; }
    }
}