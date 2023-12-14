using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JMS.FileUploader.AspNetCore
{
    public static class Extension
    {
        /// <summary>
        /// 使用JmsUploader实现上传功能
        /// app.UseJmsFileUploader() 应放在 app.UseAuthorization(); 等身份验证语句的后面
        /// </summary>
        /// <param name="app"></param>
        /// <param name="maxFileSize">最大允许上传的文件大小，默认1g</param>
        /// <returns></returns>
        public static IApplicationBuilder UseJmsFileUploader(this IApplicationBuilder app, long maxFileSize = 1024 * 1024 * 1024)
        {
            UploadReception.MaxFileSize = maxFileSize;
            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.TryGetValue("Jack-Upload-Length", out StringValues o))
                {
                    if (o.ToString().Contains(",") == false)
                    {
                        
                        var uploadingInfos = UploadReception.GetUploadingInfo(context).OrderBy(m=>m.FileItemIndex);
                        context.Request.Headers["Name"] = uploadingInfos.Select(m=>m.Name).ToArray();
                        context.Request.Headers.Add("FilePath", uploadingInfos.Select(m => m.FilePath).ToArray());
                        await next();
                        foreach (var item in uploadingInfos)
                        {
                            item.DeleteFile();
                        }
                    }
                    else
                    {
                        await UploadReception.HandleUpload(context, o);
                    }
                }
                else
                    await next();
            });
            return app;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddFileUploadFilter<T>(this IServiceCollection services) where T : IUploadFilter
        {
            return services.AddTransient(typeof(IUploadFilter), typeof(T));
        }
    }

    public interface IUploadFilter  
    {
        void OnUploadError();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="uploadId"></param>
        /// <param name="fileName"></param>
        /// <param name="fileSize"></param>
        /// <param name="fileItemIndex">同时上传多个文件时，此变量表示文件的排序号</param>
        /// <returns></returns>
        Task OnUploadBeginAsync(HttpContext context, string uploadId, string fileName, long fileSize, int fileItemIndex);
        /// <summary>
        /// 收到上传的文件数据
        /// </summary>
        /// <param name="context"></param>
        /// <param name="inputStream"></param>
        /// <param name="position"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        Task OnReceivedAsync(HttpContext context, Stream inputStream, long position, int size);
        Task OnUploadCompletedAsync(HttpContext context);
    }
}
