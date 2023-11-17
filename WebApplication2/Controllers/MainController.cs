using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MainController : ControllerBase
    {

        public MainController()
        {
        }

        [HttpGet]
        public string test()
        {
            return "abc";
        }

        [HttpPost]
        public string Test2([FromBody] object body)
        {
            var filepath = Request.Headers["FilePath"];
            var filename = Request.Headers["Name"];
            return filename;
        }
    }
}
