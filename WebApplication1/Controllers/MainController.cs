using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Xml.Linq;
using System;
using JMS.Token;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MainController : ControllerBase
    {
       
        public MainController( )
        { 
        }

        [HttpGet]
        public string test()
        {
            return "abc";
        }

        [HttpPost]
        public string Test2([FromBody]object body)
        {
            var filepath = Request.Headers["FilePath"];
            var filename = Request.Headers["Name"];
            return filename;
        }
    }
}
