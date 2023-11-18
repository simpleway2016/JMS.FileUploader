using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using JMS.FileUploader.AspNetCore;
using System.Text;
namespace WebApplication2
{
    public class Program
    {
        

        public static void Main(string[] args)
        {
           

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            var app = builder.Build();
            app.UseJmsFileUploader();
            app.UseStaticFiles();
            // Configure the HTTP request pipeline.

            app.UseAuthorization();


            app.MapControllers();

            app.Run();

           
        }
    }
}
