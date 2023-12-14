using Microsoft.AspNetCore.Mvc;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MainController : ControllerBase
    {

        [HttpPost]
        public string Test([FromBody] object body)
        {
            var customHeader = Request.Headers["Custom-Header"];

            //临时文件路径
            var filepath = Request.Headers["FilePath"];

            //文件名
            var filename = Request.Headers["Name"];
            return filepath + "\r\n" + filename + "\r\n" + customHeader;
        }
    }
}
