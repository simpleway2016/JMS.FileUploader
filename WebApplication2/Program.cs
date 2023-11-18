using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Util;
using JMS.FileUploader.AspNetCore;
using System.Text;
namespace WebApplication2
{
    public class Program
    {
        
        static void s3()
        {
            byte[] data = File.ReadAllBytes("d:\\公司测试机地址.txt");

            //提供awsAccessKeyId和awsSecretAccessKey构造凭证
            var credentials = new BasicAWSCredentials("AKIATZEPIHO46B2CGHT4", "OLtbToGvLYnCCYtl1Z6oLr0LuqMEYmhh71LmlJSK");


            //提供awsEndPoint（域名）进行访问配置
            var clientConfig = new AmazonS3Config
            {
                ServiceURL = "https://s3.ap-east-1.amazonaws.com",
                ForcePathStyle = true
            };
            var client = new AmazonS3Client(credentials, clientConfig);

            var request = new PutObjectRequest
            {
                BucketName = "bcwimg.bcwex.co",
                Key = $"test/a.txt",
                InputStream = new MemoryStream(data),
                CannedACL = S3CannedACL.PublicRead
            };
            var ret = client.PutObjectAsync(request).GetAwaiter().GetResult();
           client.PutACLAsync(new PutACLRequest
            {
                CannedACL = S3CannedACL.PublicRead,
                BucketName = request.BucketName,
                Key = request.Key,
            }).GetAwaiter().GetResult();
          var ddw =  client.GetObjectAsync(request.BucketName, request.Key).GetAwaiter().GetResult();


            //var getRet = client.GetObjectAsync("bcwimg.bcwex.co", "test/a2.txt").GetAwaiter().GetResult();
            //byte[] readdata = new byte[getRet.ContentLength];
            //int readed = getRet.ResponseStream.Read(readdata, 0, readdata.Length);

            //if (Encoding.UTF8.GetString(data) != Encoding.UTF8.GetString(readdata))
            //{
            //    throw new Exception("err");
            //}

            var key = "test/a.zip";
            var initRet = client.InitiateMultipartUploadAsync("bcwimg.bcwex.co", key).GetAwaiter().GetResult();

            List<PartETag> partETags = new List<PartETag>();
            var partNum = 1;
            for (var i = 0; i < data.Length;)
            {

                UploadPartRequest partRequest = new UploadPartRequest();
                partRequest.BucketName = "bcwimg.bcwex.co";
                partRequest.UploadId = initRet.UploadId;
                partRequest.Key = key;
                partRequest.PartNumber = partNum++;


                partRequest.FilePosition = i;
                //亚马逊限制每段最小5M
                partRequest.InputStream = new MemoryStream(data, i, Math.Min(data.Length - i, 1024 * 1024 * 5));
                partRequest.IsLastPart = (data.Length - i) < 1024 * 1024 * 5;
                i += (int)partRequest.InputStream.Length;
                var ret1 = client.UploadPartAsync(partRequest).GetAwaiter().GetResult();
                partETags.Add(new PartETag(ret1));
            }


            var ret3 = client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
            {
                UploadId = initRet.UploadId,
                BucketName = "bcwimg.bcwex.co",
                Key = key,
                PartETags = partETags,
            }).GetAwaiter().GetResult();

            client.PutACLAsync(new PutACLRequest
            {
                CannedACL = S3CannedACL.PublicRead,
                BucketName = ret3.BucketName,
                Key = ret3.Key,
            }).GetAwaiter().GetResult();
        }
        public static void Main(string[] args)
        {
            s3();

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
