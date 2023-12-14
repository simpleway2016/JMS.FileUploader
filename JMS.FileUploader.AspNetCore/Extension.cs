using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            var type = typeof(T);
            var attr = type.GetCustomAttribute<UploadFilterDescriptionAttribute>();
            if(attr == null || string.IsNullOrWhiteSpace(attr.Description))
            {
                UploadReception.Filters[""] = type;
            }
            else
            {
                UploadReception.Filters[attr.Description.Trim()] = type;
            }

            services.AddSingleton(type);
            return services;
        }
    }

   
}
