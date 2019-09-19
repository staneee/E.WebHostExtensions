using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TestApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        static int count = 0;

        // GET api/values
        [HttpGet]
        public ActionResult<string> Get()
        {
            return $"hello {count++}";
        }

    }
}
