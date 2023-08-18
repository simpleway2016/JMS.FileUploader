using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JMS.FileUploader.AspNetCore
{
    internal static class Uploader
    {
        internal const int MaxBlockSize = 1024000;
        internal static long MaxFileSize;
        static ConcurrentDictionary<string, UploadingInfo> _ReceivingDict = new ConcurrentDictionary<string, UploadingInfo>();
        static Uploader()
        {
            try
            {
                var files = Directory.GetFiles(UploadingInfo.RootFolder);
                foreach (var file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception)
            {

            }
            new Thread(CheckOutTime).Start();
        }
        static void CheckOutTime()
        {
            while (true)
            {
                Thread.Sleep(60000);
                try
                {
                    foreach (var pair in _ReceivingDict)
                    {
                        if ((DateTime.Now - pair.Value._lastReceiveTime).TotalMinutes > 10)
                        {
                            if (_ReceivingDict.TryRemove(pair.Key, out UploadingInfo o))
                            {
                                o.Dispose();
                                try
                                {
                                    if (File.Exists(o.FilePath))
                                    {
                                        File.SetAttributes(o.FilePath, FileAttributes.Normal);
                                        File.Delete(o.FilePath);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }
     
       

        internal static UploadingInfo GetUploadingInfo(HttpContext context)
        {
            string fileName = context.Request.Headers["Name"].FirstOrDefault();
            string uploadId = context.Request.Headers["Upload-Id"].FirstOrDefault();

            return _ReceivingDict[$"{fileName},{uploadId}"];
        }


        public static async Task HandleUpload(HttpContext context, StringValues filelen)
        {
            var arr = filelen.ToString().Split(',');
            var length = long.Parse(arr[0]);
            var position = long.Parse(arr[1]);
            var blockSize = int.Parse(arr[2]);

            if (blockSize > MaxBlockSize || context.Request.ContentLength > MaxBlockSize || context.Request.ContentLength == null)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("文件大小不正确");
                return;
            }
            if (length > MaxFileSize)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("文件大小超出限制");
                return;
            }

            string fileName = context.Request.Headers["Name"].FirstOrDefault();
            string uploadId = context.Request.Headers["Upload-Id"].FirstOrDefault();

            var uploadingInfo = _ReceivingDict.GetOrAdd($"{fileName},{uploadId}", k => new UploadingInfo(fileName, length , context.RequestServices.GetService<IUploadFilter>()));

            if (uploadingInfo.Completed)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("无效的文件块");
                return;
            }

            uploadingInfo.Init();

            await uploadingInfo.Receive(context ,fileName, length, position, context.Request.Body , blockSize);
            await context.Response.WriteAsync("ok");
        }
    }

   

   
}
