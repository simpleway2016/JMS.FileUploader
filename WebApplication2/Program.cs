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
        static void test()
        {
            var content = File.ReadAllText("a.txt").FromJson<Info>();
            var client = new OssClient(content.Url, content.Key, content.Id);
            var bucketName = "domainconfig";
            var key = "domain/a.zip";
            var initiateMultipartUploadResult = client.InitiateMultipartUpload(new InitiateMultipartUploadRequest(bucketName, key));
            var inputStream = File.OpenRead("d:\\app store.zip");

            long position = 0;
            while (true)
            {
                var data = new byte[102400];
                var readed = inputStream.Read(data, 0, data.Length);
                if (readed == 0)
                    break;

                using var ms = new MemoryStream(data);

              
                var num = (int)(position / 102400) + 1;

                position += readed;

                var ret = client.UploadPart(new UploadPartRequest(bucketName, key, initiateMultipartUploadResult.UploadId)
                {
                    InputStream = ms,

                    PartSize = readed,
                    PartNumber = num
                });

                if (ret.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception(ret.HttpStatusCode.ToString());
            }

            // 列出所有分块。
            var listPartsRequest = new ListPartsRequest(bucketName, key, initiateMultipartUploadResult.UploadId);
            var partList = client.ListParts(listPartsRequest);

            // 创建CompleteMultipartUploadRequest对象。
            var completeRequest = new CompleteMultipartUploadRequest(bucketName, key, initiateMultipartUploadResult.UploadId);

            // 设置分块列表。
            foreach (var part in partList.Parts)
            {
                completeRequest.PartETags.Add(new PartETag(part.PartNumber, part.ETag));
            }

            // 完成上传。
            var ret2 = client.CompleteMultipartUpload(completeRequest);


            if (ret2.HttpStatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(ret2.HttpStatusCode.ToString());
        }

        public static void Main(string[] args)
        {
            Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;
           // test();
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
