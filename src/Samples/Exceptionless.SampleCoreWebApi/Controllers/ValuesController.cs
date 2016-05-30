using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Exceptionless.SampleAspNetCore.Controllers
    {
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        [HttpGet("{id}")]
        public Task<string> Get(int id)
            {
            // throw an exception to simulate an unhandled exception.
            if (id == 1)
                {
                throw new UnauthorizedAccessException("No such user exists for login.");
                }

            if (id == 2)
                {
                throw new ValidationException("Missing user name.");
                }

            throw new ArgumentNullException(nameof(id));
            }
        }
}
