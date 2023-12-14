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

            //��ʱ�ļ�·��
            var filepath = Request.Headers["FilePath"];

            //�ļ���
            var filename = Request.Headers["Name"];
            return filepath + "\r\n" + filename + "\r\n" + customHeader;
        }
    }
}
