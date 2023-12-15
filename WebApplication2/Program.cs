using Aliyun.OSS;
using JMS.FileUploader.AspNetCore;
using Org.BouncyCastle.Utilities.Zlib;
using System.Drawing;
using System.Text;
using Way.Lib;
using static Aliyun.OSS.Model.ListMultipartUploadsResult;
using static System.Net.Mime.MediaTypeNames;
namespace WebApplication2
{
    public class Program
    {

        public static void Main(string[] args)
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddFileUploadFilter<AliyunUploadFilter>();

            var app = builder.Build();
           
            app.UseStaticFiles();
            // Configure the HTTP request pipeline.

            app.UseAuthorization();


            app.MapControllers();

            app.UseJmsFileUploader();
            app.Run();

           
        }
    }
}
